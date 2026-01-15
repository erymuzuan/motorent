using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Repository for Core schema entities (Organization, User, Setting, etc.).
/// Uses the [Core] schema instead of tenant-specific schemas.
/// </summary>
public class CoreRepository<T> : IRepository<T> where T : Entity
{
    private readonly QueryProvider m_provider;
    private readonly string m_tableName;
    private readonly string m_idColumn;

    public CoreRepository(QueryProvider provider)
    {
        m_provider = provider;
        m_tableName = $"[Core].[{typeof(T).Name}]";
        m_idColumn = $"{typeof(T).Name}Id";
    }

    public async Task<T?> LoadOneAsync(IQueryable<T> query)
    {
        var result = await LoadAsync(query, page: 1, size: 1, includeTotalRows: false);
        return result.ItemCollection.FirstOrDefault();
    }

    public async Task<T?> LoadOneAsync(Expression<Func<T, bool>> predicate)
    {
        var query = new Query<T>(m_provider).Where(predicate);
        return await LoadOneAsync(query);
    }

    public async Task<LoadOperation<T>> LoadAsync(IQueryable<T> query, int page = 1, int size = 40, bool includeTotalRows = false)
    {
        var result = new LoadOperation<T>
        {
            Page = page,
            PageSize = size
        };

        var typedQuery = query as Query<T>;
        if (typedQuery == null)
        {
            return result;
        }

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        // Build WHERE clause
        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var orderClause = BuildOrderClause(typedQuery.OrderByColumns);

        // Get total count if requested
        if (includeTotalRows)
        {
            var countSql = $"SELECT COUNT(*) FROM {m_tableName} {whereClause}";
            await using var countCmd = new SqlCommand(countSql, connection);
            AddWhereParameters(countCmd, typedQuery.Predicates);
            result.TotalRows = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
        }

        // Build main query with pagination
        var offset = (page - 1) * size;
        var sql = $@"
            SELECT [{m_idColumn}], [Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp]
            FROM {m_tableName}
            {whereClause}
            {orderClause}
            OFFSET {offset} ROWS FETCH NEXT {size} ROWS ONLY";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var entity = MapFromReader(reader);
            if (entity != null)
                result.ItemCollection.Add(entity);
        }

        return result;
    }

    public async Task<int> InsertAsync(T entity, string username)
    {
        if (string.IsNullOrWhiteSpace(entity.WebId))
            entity.WebId = Guid.NewGuid().ToString();

        var json = entity.ToJsonString();
        var now = DateTimeOffset.Now;

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var sql = $@"
            INSERT INTO {m_tableName} ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
            OUTPUT INSERTED.[{m_idColumn}]
            VALUES (@Json, @Username, @Username, @Timestamp, @Timestamp)";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Json", json);
        cmd.Parameters.AddWithValue("@Username", username);
        cmd.Parameters.AddWithValue("@Timestamp", now);

        var id = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        entity.SetId(id);
        entity.CreatedBy = username;
        entity.ChangedBy = username;
        entity.CreatedTimestamp = now;
        entity.ChangedTimestamp = now;

        return id;
    }

    public async Task<int> UpdateAsync(T entity, string username)
    {
        var json = entity.ToJsonString();
        var now = DateTimeOffset.Now;

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var sql = $@"
            UPDATE {m_tableName}
            SET [Json] = @Json, [ChangedBy] = @Username, [ChangedTimestamp] = @Timestamp
            WHERE [{m_idColumn}] = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Json", json);
        cmd.Parameters.AddWithValue("@Username", username);
        cmd.Parameters.AddWithValue("@Timestamp", now);
        cmd.Parameters.AddWithValue("@Id", entity.GetId());

        entity.ChangedBy = username;
        entity.ChangedTimestamp = now;

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteAsync(T entity)
    {
        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var sql = $"DELETE FROM {m_tableName} WHERE [{m_idColumn}] = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", entity.GetId());

        return await cmd.ExecuteNonQueryAsync();
    }

    #region Aggregate Methods

    public async Task<int> GetCountAsync(IQueryable<T> query)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return 0;

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT COUNT(*) FROM {m_tableName} {whereClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        return (int)(await cmd.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> ExistAsync(IQueryable<T> query)
    {
        var count = await GetCountAsync(query);
        return count > 0;
    }

    public async Task<TResult> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector) where TResult : struct
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return default;

        var columnName = GetColumnNameFromSelector(selector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT ISNULL(SUM([{columnName}]), 0) FROM {m_tableName} {whereClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? default : (TResult)Convert.ChangeType(result, typeof(TResult));
    }

    public async Task<TResult> GetMaxAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return default!;

        var columnName = GetColumnNameFromSelector(selector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT MAX([{columnName}]) FROM {m_tableName} {whereClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? default! : (TResult)Convert.ChangeType(result, typeof(TResult));
    }

    public async Task<TResult> GetMinAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return default!;

        var columnName = GetColumnNameFromSelector(selector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT MIN([{columnName}]) FROM {m_tableName} {whereClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? default! : (TResult)Convert.ChangeType(result, typeof(TResult));
    }

    public async Task<decimal> GetAverageAsync(IQueryable<T> query, Expression<Func<T, decimal>> selector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return 0m;

        var columnName = GetColumnNameFromSelector(selector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT ISNULL(AVG(CAST([{columnName}] AS DECIMAL(18,4))), 0) FROM {m_tableName} {whereClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
    }

    public async Task<TResult?> GetScalarAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return default;

        var columnName = GetColumnNameFromSelector(selector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var orderClause = BuildOrderClause(typedQuery.OrderByColumns);
        var sql = $"SELECT TOP 1 [{columnName}] FROM {m_tableName} {whereClause} {orderClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? default : (TResult)Convert.ChangeType(result, typeof(TResult));
    }

    public async Task<List<T>> GetListAsync(IQueryable<T> query, Expression<Func<T, object>> selector)
    {
        var result = await LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<List<TResult>> GetDistinctAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return [];

        var columnName = GetColumnNameFromSelector(selector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT DISTINCT [{columnName}] FROM {m_tableName} {whereClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var results = new List<TResult>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var value = reader.GetValue(0);
            if (value != null && value != DBNull.Value)
            {
                results.Add((TResult)Convert.ChangeType(value, typeof(TResult)));
            }
        }
        return results;
    }

    public async Task<TResult?> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult?>> selector) where TResult : struct
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return default;

        var columnName = GetColumnNameFromSelector(selector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT SUM([{columnName}]) FROM {m_tableName} {whereClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? null : (TResult?)Convert.ChangeType(result, typeof(TResult));
    }

    public async Task<List<(TKey Key, int Count)>> GetGroupCountAsync<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return [];

        var keyColumn = GetColumnNameFromSelector(keySelector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT [{keyColumn}], COUNT(*) FROM {m_tableName} {whereClause} GROUP BY [{keyColumn}]";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var results = new List<(TKey, int)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var keyRaw = reader.GetValue(0);
            var countRaw = reader.GetValue(1);

            var key = ConvertToType<TKey>(keyRaw);
            if (key != null && countRaw is int count)
            {
                results.Add((key, count));
            }
        }
        return results;
    }

    public async Task<List<(TKey Key, TValue Sum)>> GetGroupSumAsync<TKey, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TValue>> valueSelector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return [];

        var keyColumn = GetColumnNameFromSelector(keySelector);
        var valueColumn = GetColumnNameFromSelector(valueSelector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT [{keyColumn}], SUM([{valueColumn}]) FROM {m_tableName} {whereClause} GROUP BY [{keyColumn}]";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var results = new List<(TKey, TValue)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var keyRaw = reader.GetValue(0);
            var sumRaw = reader.GetValue(1);

            var key = ConvertToType<TKey>(keyRaw);
            var sum = ConvertToType<TValue>(sumRaw);
            if (key != null && sum != null)
            {
                results.Add((key, sum));
            }
        }
        return results;
    }

    public async Task<List<(TKey Key, TKey2 Key2, TValue Sum)>> GetGroupSumAsync<TKey, TKey2, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TKey2>> key2Selector,
        Expression<Func<T, TValue>> valueSelector)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return [];

        var keyColumn = GetColumnNameFromSelector(keySelector);
        var key2Column = GetColumnNameFromSelector(key2Selector);
        var valueColumn = GetColumnNameFromSelector(valueSelector);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var sql = $"SELECT [{keyColumn}], [{key2Column}], SUM([{valueColumn}]) FROM {m_tableName} {whereClause} GROUP BY [{keyColumn}], [{key2Column}]";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var results = new List<(TKey, TKey2, TValue)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var keyRaw = reader.GetValue(0);
            var key2Raw = reader.GetValue(1);
            var sumRaw = reader.GetValue(2);

            var key = ConvertToType<TKey>(keyRaw);
            var key2 = ConvertToType<TKey2>(key2Raw);
            var sum = ConvertToType<TValue>(sumRaw);
            if (key != null && key2 != null && sum != null)
            {
                results.Add((key, key2, sum));
            }
        }
        return results;
    }

    public async Task<List<(TResult, TResult2)>> GetList2Async<TResult, TResult2>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return [];

        var column1 = GetColumnNameFromSelector(selector);
        var column2 = GetColumnNameFromSelector(selector2);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var orderClause = BuildOrderClause(typedQuery.OrderByColumns);
        var sql = $"SELECT [{column1}], [{column2}] FROM {m_tableName} {whereClause} {orderClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var results = new List<(TResult, TResult2)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var val1 = ConvertToType<TResult>(reader.GetValue(0));
            var val2 = ConvertToType<TResult2>(reader.GetValue(1));
            if (val1 != null && val2 != null)
            {
                results.Add((val1, val2));
            }
        }
        return results;
    }

    public async Task<List<(TResult, TResult2, TResult3)>> GetList3Async<TResult, TResult2, TResult3>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2,
        Expression<Func<T, TResult3>> selector3)
    {
        var typedQuery = query as Query<T>;
        if (typedQuery == null) return [];

        var column1 = GetColumnNameFromSelector(selector);
        var column2 = GetColumnNameFromSelector(selector2);
        var column3 = GetColumnNameFromSelector(selector3);

        await using var connection = m_provider.CreateConnection();
        await connection.OpenAsync();

        var whereClause = BuildWhereClause(typedQuery.Predicates);
        var orderClause = BuildOrderClause(typedQuery.OrderByColumns);
        var sql = $"SELECT [{column1}], [{column2}], [{column3}] FROM {m_tableName} {whereClause} {orderClause}";

        await using var cmd = new SqlCommand(sql, connection);
        AddWhereParameters(cmd, typedQuery.Predicates);

        var results = new List<(TResult, TResult2, TResult3)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var val1 = ConvertToType<TResult>(reader.GetValue(0));
            var val2 = ConvertToType<TResult2>(reader.GetValue(1));
            var val3 = ConvertToType<TResult3>(reader.GetValue(2));
            if (val1 != null && val2 != null && val3 != null)
            {
                results.Add((val1, val2, val3));
            }
        }
        return results;
    }

    /// <summary>
    /// Converts a database value to the target type, handling enums, DateOnly, and nullable types.
    /// </summary>
    private static TTarget? ConvertToType<TTarget>(object? value)
    {
        if (value == null || value == DBNull.Value) return default;

        var targetType = typeof(TTarget);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle enums
        if (underlyingType.IsEnum)
        {
            if (Enum.TryParse(underlyingType, value.ToString(), out var enumValue))
                return (TTarget)enumValue!;
            return default;
        }

        // Handle DateOnly
        if (underlyingType == typeof(DateOnly) && value is DateTime dt)
            return (TTarget)(object)DateOnly.FromDateTime(dt);

        // Handle TimeOnly
        if (underlyingType == typeof(TimeOnly) && value is TimeSpan ts)
            return (TTarget)(object)TimeOnly.FromTimeSpan(ts);

        // Standard conversion
        try
        {
            return (TTarget)Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            return default;
        }
    }

    private static string GetColumnNameFromSelector<TResult>(Expression<Func<T, TResult>> selector)
    {
        if (selector.Body is MemberExpression member)
            return member.Member.Name;
        if (selector.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            return unaryMember.Member.Name;
        throw new ArgumentException("Invalid selector expression");
    }

    #endregion

    private T? MapFromReader(SqlDataReader reader)
    {
        var json = reader.GetString(reader.GetOrdinal("Json"));
        var entity = json.DeserializeFromJson<T>();

        if (entity != null)
        {
            entity.SetId(reader.GetInt32(reader.GetOrdinal(m_idColumn)));
            entity.CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy"));
            entity.ChangedBy = reader.GetString(reader.GetOrdinal("ChangedBy"));
            entity.CreatedTimestamp = reader.GetDateTimeOffset(reader.GetOrdinal("CreatedTimestamp"));
            entity.ChangedTimestamp = reader.GetDateTimeOffset(reader.GetOrdinal("ChangedTimestamp"));
        }

        return entity;
    }

    private string BuildWhereClause(List<Expression<Func<T, bool>>> predicates)
    {
        if (predicates.Count == 0)
            return string.Empty;

        var conditions = new List<string>();
        for (int i = 0; i < predicates.Count; i++)
        {
            var condition = ParsePredicate(predicates[i], i);
            if (!string.IsNullOrEmpty(condition))
                conditions.Add(condition);
        }

        return conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty;
    }

    private string ParsePredicate(Expression<Func<T, bool>> predicate, int paramIndex)
    {
        if (predicate.Body is BinaryExpression binary)
        {
            var left = GetMemberName(binary.Left);
            var op = GetSqlOperator(binary.NodeType);

            if (left != null)
                return $"[{left}] {op} @p{paramIndex}";
        }

        if (predicate.Body is MethodCallExpression methodCall)
        {
            if (methodCall.Method.Name == "Contains" && methodCall.Object is MemberExpression member)
            {
                return $"[{member.Member.Name}] LIKE @p{paramIndex}";
            }

            // Handle IsInList extension method for SQL IN clause
            // Usage: ids.IsInList(e.PropertyName) -> [PropertyName] IN (...)
            if (methodCall.Method.Name == "IsInList" && methodCall.Arguments.Count == 2)
            {
                var listArg = methodCall.Arguments[0];
                var itemArg = methodCall.Arguments[1];
                var columnName = GetMemberName(itemArg);
                if (columnName != null)
                {
                    var values = GetIsInListValues(listArg);
                    if (values != null && values.Length > 0)
                    {
                        var paramNames = Enumerable.Range(0, values.Length)
                            .Select(i => $"@p{paramIndex}_{i}")
                            .ToArray();
                        return $"[{columnName}] IN ({string.Join(", ", paramNames)})";
                    }
                    return "1=0"; // Empty list - return condition that matches nothing
                }
            }
        }

        return string.Empty;
    }

    private object[]? GetIsInListValues(Expression? expression)
    {
        if (expression == null) return null;

        var value = GetValue(expression);
        if (value == null) return null;

        // Handle various enumerable types
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                if (item != null)
                    list.Add(item);
            }
            return list.ToArray();
        }

        return null;
    }

    private string? GetMemberName(Expression expression)
    {
        if (expression is MemberExpression member)
            return member.Member.Name;
        if (expression is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            return unaryMember.Member.Name;
        return null;
    }

    private string GetSqlOperator(ExpressionType nodeType) => nodeType switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        _ => "="
    };

    private void AddWhereParameters(SqlCommand cmd, List<Expression<Func<T, bool>>> predicates)
    {
        for (int i = 0; i < predicates.Count; i++)
        {
            if (predicates[i].Body is MethodCallExpression methodCall)
            {
                // Handle IsInList - add multiple parameters for IN clause
                if (methodCall.Method.Name == "IsInList" && methodCall.Arguments.Count == 2)
                {
                    var listArg = methodCall.Arguments[0];
                    var values = GetIsInListValues(listArg);
                    if (values != null)
                    {
                        for (int j = 0; j < values.Length; j++)
                        {
                            var paramValue = values[j].GetType().IsEnum ? values[j].ToString() : values[j];
                            cmd.Parameters.AddWithValue($"@p{i}_{j}", paramValue);
                        }
                    }
                    continue;
                }

                // Handle string.Contains - add LIKE parameter with wildcards
                if (methodCall.Method.Name == "Contains")
                {
                    var value = GetPredicateValue(predicates[i]);
                    if (value != null)
                    {
                        var paramValue = value.GetType().IsEnum ? value.ToString() : value;
                        cmd.Parameters.AddWithValue($"@p{i}", $"%{paramValue}%");
                    }
                    continue;
                }
            }

            // Handle binary expressions and other cases
            var binaryValue = GetPredicateValue(predicates[i]);
            if (binaryValue != null)
            {
                // Convert enum values to string for NVARCHAR comparison in SQL
                var paramValue = binaryValue.GetType().IsEnum ? binaryValue.ToString() : binaryValue;
                cmd.Parameters.AddWithValue($"@p{i}", paramValue);
            }
        }
    }

    private object? GetPredicateValue(Expression<Func<T, bool>> predicate)
    {
        if (predicate.Body is BinaryExpression binary)
        {
            return GetValue(binary.Right);
        }
        if (predicate.Body is MethodCallExpression methodCall && methodCall.Arguments.Count > 0)
        {
            return GetValue(methodCall.Arguments[0]);
        }
        return null;
    }

    private object? GetValue(Expression expression)
    {
        if (expression is ConstantExpression constant)
            return constant.Value;

        if (expression is MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        if (expression is UnaryExpression unary)
            return GetValue(unary.Operand);

        return null;
    }

    private string BuildOrderClause(List<(string Column, bool Descending)> orderBy)
    {
        if (orderBy.Count == 0)
            return $"ORDER BY [{m_idColumn}] DESC";

        var clauses = orderBy.Select(o => $"[{o.Column}] {(o.Descending ? "DESC" : "ASC")}");
        return $"ORDER BY {string.Join(", ", clauses)}";
    }
}
