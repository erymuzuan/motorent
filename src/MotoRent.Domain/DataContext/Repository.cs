using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public class Repository<T> : IRepository<T> where T : Entity
{
    private readonly QueryProvider m_provider;
    private readonly string m_tableName;
    private readonly string m_idColumn;

    public Repository(QueryProvider provider)
    {
        m_provider = provider;
        m_tableName = $"[MotoRent].[{typeof(T).Name}]";
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
        // This loads full entities - use LoadAsync with paging for large datasets
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
        }

        return string.Empty;
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
            var value = GetPredicateValue(predicates[i]);
            if (value != null)
            {
                if (predicates[i].Body is MethodCallExpression methodCall && methodCall.Method.Name == "Contains")
                    cmd.Parameters.AddWithValue($"@p{i}", $"%{value}%");
                else
                    cmd.Parameters.AddWithValue($"@p{i}", value);
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
