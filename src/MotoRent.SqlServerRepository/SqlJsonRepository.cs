using System.Data;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using Polly;

namespace MotoRent.SqlServerRepository;

/// <summary>
/// SQL JSON Repository implementation with Polly retry policies for transient failure resilience.
/// Uses proper LINQ expression tree translation via TsqlQueryFormatter.
/// </summary>
public class SqlJsonRepository<T>(
    IRequestContext context,
    IPagingTranslator pagingTranslator,
    ISqlServerMetadata metadata) : IRepository<T> where T : Entity, new()
{
    private IRequestContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));
    private IPagingTranslator PagingTranslator { get; } = pagingTranslator ?? throw new ArgumentNullException(nameof(pagingTranslator));
    private ISqlServerMetadata Metadata { get; } = metadata ?? throw new ArgumentNullException(nameof(metadata));
    private string IdColumn { get; } = $"{typeof(T).Name}Id";
    private string TableName { get; } = typeof(T).Name;

    private static bool HasNetworkError(SqlException exception) => exception switch
    {
        null => false,
        { Number: 40613 } => true,
        { Message.Length: > 0 } when exception.Message.Contains("deadlocked") => true,
        { Message.Length: > 0 } when exception.Message.Contains("timeout") => true,
        { Message.Length: > 0 } when exception.Message.Contains("Please retry the connection later") => true,
        _ => false
    };

    private static TimeSpan Sleep(int c) => TimeSpan.FromMilliseconds(600 * Math.Pow(2, c));

    private string GetSchema() => this.Context.GetSchema() ?? "MotoRent";

    private string GetTableName() => $"[{this.GetSchema()}].[{this.TableName}]";

    public IQueryable<T> CreateQuery()
    {
        var provider = new SqlQueryProvider(this.Context);
        return new Query<T>(provider);
    }

    public async Task<T?> LoadOneAsync(IQueryable<T> query)
    {
        var result = await this.LoadAsync(query, page: 1, size: 1, includeTotalRows: false);
        return result.ItemCollection.FirstOrDefault();
    }

    public async Task<T?> LoadOneAsync(Expression<Func<T, bool>> predicate)
    {
        var query = this.CreateQuery().Where(predicate);
        return await this.LoadOneAsync(query);
    }

public async Task<LoadOperation<T>> LoadAsync(IQueryable<T> query, int page = 1, int size = 40, bool includeTotalRows = false)
    {
        var schema = this.GetSchema();
        var sql = this.GenerateSqlWithComputedColumns(query);
        var pagedSql = string.Empty;

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var result = new LoadOperation<T>
                {
                    Page = page,
                    PageSize = size
                };

                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await conn.OpenAsync();

// Get total count if requested
                if (includeTotalRows)
                {
                    // For COUNT, we don't need computed columns, use standard method
                    var countSql = query.ToSqlCommand("COUNT(*)");
                    await using var countCmd = new SqlCommand(countSql, conn);
                    result.TotalRows = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
                }

// Apply paging
                pagedSql = this.ApplyPagingWithComputedColumns(sql, page, size);

                await using var cmd = new SqlCommand(pagedSql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var entity = this.MapFromReader(reader);
                    if (entity is not null)
                        result.ItemCollection.Add(entity);
                }

                return result;
            });

        if (await pr.FinalException.CreateSqlObjectIfMissingAsync(schema, this.TableName))
            return await this.LoadAsync(query, page, size, includeTotalRows);

        if (pr.FinalException != null)
        {
            System.Diagnostics.Debug.WriteLine($"SQL Error for {typeof(T).Name}: {pagedSql}");
            Console.WriteLine($"SQL Error for {typeof(T).Name}: {pagedSql}");
            throw new InvalidOperationException($"SQL Error for {typeof(T).Name}: {pagedSql}", pr.FinalException);
        }

        return pr.Result;
    }

    public async Task<int> InsertAsync(T entity, string username)
    {
        if (string.IsNullOrWhiteSpace(entity.WebId))
            entity.WebId = Guid.NewGuid().ToString();

        var schema = this.GetSchema();
        var table = await this.Metadata.GetTableAsync(schema, this.TableName);
        if (table is null)
        {
            if (await new InvalidOperationException($"Table [{schema}].[{this.TableName}] not found").CreateSqlObjectIfMissingAsync(schema, this.TableName))
                table = await this.Metadata.GetTableAsync(schema, this.TableName);
            if (table is null)
                throw new InvalidOperationException($"Table [{schema}].[{this.TableName}] not found");
        }

        var columns = table.Columns.Where(c => !c.IsIdentity && c.CanWrite).ToArray();
        var json = entity.ToJsonString();
        var now = DateTimeOffset.Now;

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await conn.OpenAsync();

                var columnNames = string.Join(",", columns.Select(c => $"[{c.Name}]"));
                var paramNames = string.Join(",", columns.Select((c, i) => $"@p{i}"));

                var sql = $@"
                    INSERT INTO {this.GetTableName()} ({columnNames})
                    OUTPUT INSERTED.[{this.IdColumn}]
                    VALUES ({paramNames})";

                await using var cmd = new SqlCommand(sql, conn);

                for (var i = 0; i < columns.Length; i++)
                {
                    var col = columns[i];
                    var value = this.GetParameterValue(col, entity, json, username, now, true);
                    cmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
                }

                return (int)(await cmd.ExecuteScalarAsync() ?? 0);
            });

        if (pr.FinalException is not null)
        {
            if (await pr.FinalException.CreateSqlObjectIfMissingAsync(schema, this.TableName))
                return await this.InsertAsync(entity, username);
            throw pr.FinalException;
        }

        entity.SetId(pr.Result);
        entity.CreatedBy = username;
        entity.ChangedBy = username;
        entity.CreatedTimestamp = now;
        entity.ChangedTimestamp = now;

        return pr.Result;
    }

    public async Task<int> UpdateAsync(T entity, string username)
    {
        var schema = this.GetSchema();
        var table = await this.Metadata.GetTableAsync(schema, this.TableName);
        if (table is null)
            throw new InvalidOperationException($"Table [{schema}].[{this.TableName}] not found");

        var columns = table.Columns
            .Where(c => !c.IsIdentity && c.CanWrite)
            .Where(c => c.Name != "CreatedBy" && c.Name != "CreatedDate" && c.Name != "CreatedTimestamp")
            .ToArray();

        var json = entity.ToJsonString();
        var now = DateTimeOffset.Now;
        var id = entity.GetId();

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await conn.OpenAsync();

                var setClause = string.Join(",", columns.Select((c, i) => $"[{c.Name}]=@p{i}"));
                var sql = $@"
                    UPDATE {this.GetTableName()}
                    SET {setClause}
                    WHERE [{this.IdColumn}] = @Id";

                await using var cmd = new SqlCommand(sql, conn);

                for (var i = 0; i < columns.Length; i++)
                {
                    var col = columns[i];
                    var value = this.GetParameterValue(col, entity, json, username, now, false);
                    cmd.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
                }
                cmd.Parameters.AddWithValue("@Id", id);

                return await cmd.ExecuteNonQueryAsync();
            });

        if (pr.FinalException is not null)
        {
            if (await pr.FinalException.CreateSqlObjectIfMissingAsync(schema, this.TableName))
                return await this.UpdateAsync(entity, username);
            throw pr.FinalException;
        }

        entity.ChangedBy = username;
        entity.ChangedTimestamp = now;

        return pr.Result;
    }

    public async Task<int> DeleteAsync(T entity)
    {
        var schema = this.GetSchema();
        var id = entity.GetId();

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await conn.OpenAsync();

                var sql = $"DELETE FROM {this.GetTableName()} WHERE [{this.IdColumn}] = @Id";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                return await cmd.ExecuteNonQueryAsync();
            });

        if (pr.FinalException is not null)
        {
            if (await pr.FinalException.CreateSqlObjectIfMissingAsync(schema, this.TableName))
                return await this.DeleteAsync(entity);
            throw pr.FinalException;
        }

        return pr.Result;
    }

    private object? GetParameterValue(Column col, T entity, string json, string username, DateTimeOffset now, bool isInsert) => col.Name switch
    {
        "Json" => json,
        "CreatedDate" when isInsert => DateTime.Now,
        "CreatedTimestamp" when isInsert => now,
        "CreatedBy" when isInsert => username,
        "ChangedDate" => DateTime.Now,
        "ChangedTimestamp" => now,
        "ChangedBy" => username,
        _ => GetEntityPropertyValue(col, entity)
    };

    private static object? GetEntityPropertyValue(Column column, T entity)
    {
        var prop = typeof(T).GetProperty(column.Name!);
        if (prop is null)
            return DBNull.Value;

        var value = prop.GetValue(entity);
        if (value is null)
            return column.IsNullable ? DBNull.Value : null;

        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        return (type.Name, value) switch
        {
            // Thai Buddhist calendar handling
            ("DateTimeOffset", DateTimeOffset { Year: > 3000 } ut) => ut.AddYears(-1086),
            ("DateTimeOffset", DateTimeOffset { Year: > 2500 } ut) => ut.AddYears(-543),
            ("DateOnly", DateOnly { Year: > 3000 } ut) => $"{ut.Year - 1086}-{ut:MM-dd}",
            ("DateOnly", DateOnly { Year: > 2500 } ut) => $"{ut.Year - 543}-{ut:MM-dd}",
            ("DateTime", DateTime { Year: 1 }) when column.IsNullable => DBNull.Value,
            ("DateOnly", DateOnly { Year: 1 }) when column.IsNullable => DBNull.Value,
            (_, DateOnly { Year: > 1920 and < 2120 } dt) => $"{dt:yyyy-MM-dd}",
            (_, null) when column.IsNullable => DBNull.Value,
            _ when type.IsEnum => value.ToString(),
            _ => value
        };
    }

    private T? MapFromReader(SqlDataReader reader)
    {
        var json = reader.GetString(reader.GetOrdinal("Json"));
        var entity = json.DeserializeFromJson<T>();

        if (entity is not null)
            entity.SetId(reader.GetInt32(reader.GetOrdinal(this.IdColumn)));

        return entity;
    }

    public async Task<int> GetCountAsync(IQueryable<T> query)
    {
        var sql = query.ToSqlCommand("COUNT(*)");
        var schema = this.GetSchema();

        return await this.GetCountAsync(sql, schema, this.TableName);
    }

    private async Task<int> GetCountAsync(string sql, string schema, string table)
    {
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            });

        if (await pr.FinalException.CreateSqlObjectIfMissingAsync(schema, table))
            return await this.GetCountAsync(sql, schema, table);

        if (pr.FinalException is not null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<bool> ExistAsync(IQueryable<T> query)
    {
        return (await this.GetCountAsync(query)) > 0;
    }

    public async Task<TResult> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector) where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToSqlCommand($"ISNULL(SUM([{column}]), 0)");
        return await this.ExecuteScalarAsync<TResult>(sql);
    }

    public async Task<TResult?> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult?>> selector) where TResult : struct
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToSqlCommand($"SUM([{column}])");
        return await this.ExecuteScalarNullableAsync<TResult>(sql);
    }

    public async Task<TResult> GetMaxAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToSqlCommand($"MAX([{column}])");
        return await this.ExecuteScalarAsync<TResult>(sql);
    }

    public async Task<TResult> GetMinAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToSqlCommand($"MIN([{column}])");
        return await this.ExecuteScalarAsync<TResult>(sql);
    }

    public async Task<decimal> GetAverageAsync(IQueryable<T> query, Expression<Func<T, decimal>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the aggregate column name");

        var sql = query.ToSqlCommand($"ISNULL(AVG(CAST([{column}] AS DECIMAL(18,4))), 0)");
        return await this.ExecuteScalarAsync<decimal>(sql);
    }

    public async Task<TResult?> GetScalarAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the scalar column name");

        var sql = query.ToSqlCommand($"TOP 1 [{column}]");
        return await this.ExecuteScalarNullableAsync<TResult>(sql);
    }

    public async Task<List<T>> GetListAsync(IQueryable<T> query, Expression<Func<T, object>> selector)
    {
        // This loads full entities - use LoadAsync with paging for large datasets
        var result = await this.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<List<TResult>> GetDistinctAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        var column = GetMemberName(selector);
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("Cannot determine the column name");

        var sql = query.ToSqlCommand($"DISTINCT [{column}]");
        return await this.ExecuteListAsync<TResult>(sql);
    }

    public async Task<List<(TKey Key, int Count)>> GetGroupCountAsync<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector)
    {
        var keyColumn = GetMemberName(keySelector);
        if (string.IsNullOrWhiteSpace(keyColumn))
            throw new ArgumentException("Cannot determine the key column name");

        var baseSql = query.ToSqlCommand($"[{keyColumn}], COUNT(*) as Cnt");
        var sql = baseSql + $" GROUP BY [{keyColumn}]";

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var results = new List<(TKey, int)>();
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var key = ConvertToType<TKey>(reader.GetValue(0));
                    var count = reader.GetInt32(1);
                    if (key != null)
                        results.Add((key, count));
                }
                return results;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<List<(TKey Key, TValue Sum)>> GetGroupSumAsync<TKey, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TValue>> valueSelector)
    {
        var keyColumn = GetMemberName(keySelector);
        var valueColumn = GetMemberName(valueSelector);
        if (string.IsNullOrWhiteSpace(keyColumn) || string.IsNullOrWhiteSpace(valueColumn))
            throw new ArgumentException("Cannot determine column names");

        var baseSql = query.ToSqlCommand($"[{keyColumn}], SUM([{valueColumn}]) as Total");
        var sql = baseSql + $" GROUP BY [{keyColumn}]";

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var results = new List<(TKey, TValue)>();
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var key = ConvertToType<TKey>(reader.GetValue(0));
                    var sum = ConvertToType<TValue>(reader.GetValue(1));
                    if (key != null && sum != null)
                        results.Add((key, sum));
                }
                return results;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<List<(TKey Key, TKey2 Key2, TValue Sum)>> GetGroupSumAsync<TKey, TKey2, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TKey2>> key2Selector,
        Expression<Func<T, TValue>> valueSelector)
    {
        var keyColumn = GetMemberName(keySelector);
        var key2Column = GetMemberName(key2Selector);
        var valueColumn = GetMemberName(valueSelector);
        if (string.IsNullOrWhiteSpace(keyColumn) || string.IsNullOrWhiteSpace(key2Column) || string.IsNullOrWhiteSpace(valueColumn))
            throw new ArgumentException("Cannot determine column names");

        var baseSql = query.ToSqlCommand($"[{keyColumn}], [{key2Column}], SUM([{valueColumn}]) as Total");
        var sql = baseSql + $" GROUP BY [{keyColumn}], [{key2Column}]";

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var results = new List<(TKey, TKey2, TValue)>();
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var key = ConvertToType<TKey>(reader.GetValue(0));
                    var key2 = ConvertToType<TKey2>(reader.GetValue(1));
                    var sum = ConvertToType<TValue>(reader.GetValue(2));
                    if (key != null && key2 != null && sum != null)
                        results.Add((key, key2, sum));
                }
                return results;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<List<(TResult, TResult2)>> GetList2Async<TResult, TResult2>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2)
    {
        var column1 = GetMemberName(selector);
        var column2 = GetMemberName(selector2);
        if (string.IsNullOrWhiteSpace(column1) || string.IsNullOrWhiteSpace(column2))
            throw new ArgumentException("Cannot determine column names");

        var sql = query.ToSqlCommand($"[{column1}], [{column2}]");

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var results = new List<(TResult, TResult2)>();
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var val1 = ConvertToType<TResult>(reader.GetValue(0));
                    var val2 = ConvertToType<TResult2>(reader.GetValue(1));
                    if (val1 != null && val2 != null)
                        results.Add((val1, val2));
                }
                return results;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<List<(TResult, TResult2, TResult3)>> GetList3Async<TResult, TResult2, TResult3>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2,
        Expression<Func<T, TResult3>> selector3)
    {
        var column1 = GetMemberName(selector);
        var column2 = GetMemberName(selector2);
        var column3 = GetMemberName(selector3);
        if (string.IsNullOrWhiteSpace(column1) || string.IsNullOrWhiteSpace(column2) || string.IsNullOrWhiteSpace(column3))
            throw new ArgumentException("Cannot determine column names");

        var sql = query.ToSqlCommand($"[{column1}], [{column2}], [{column3}]");

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var results = new List<(TResult, TResult2, TResult3)>();
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var val1 = ConvertToType<TResult>(reader.GetValue(0));
                    var val2 = ConvertToType<TResult2>(reader.GetValue(1));
                    var val3 = ConvertToType<TResult3>(reader.GetValue(2));
                    if (val1 != null && val2 != null && val3 != null)
                        results.Add((val1, val2, val3));
                }
                return results;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }


    private async Task<TResult> ExecuteScalarAsync<TResult>(string sql)
    {
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return ConvertToType<TResult>(result) ?? default!;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    private async Task<TResult?> ExecuteScalarNullableAsync<TResult>(string sql)
    {
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return ConvertToType<TResult>(result);
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    private async Task<List<TResult>> ExecuteListAsync<TResult>(string sql)
    {
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var results = new List<TResult>();
                await using var conn = new SqlConnection(this.Context.GetConnectionString());
                await using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var val = ConvertToType<TResult>(reader.GetValue(0));
                    if (val != null)
                        results.Add(val);
                }
                return results;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    private static string? GetMemberName<TResult>(Expression<Func<T, TResult>> selector)
    {
        if (selector is not LambdaExpression) return null;
        var body = selector.Body as MemberExpression;
        if (body is { Expression: MemberExpression me2, Member.Name.Length: > 0 })
        {
            return $"{me2.Member.Name}.{body.Member.Name}";
        }

        if (body != null) return body.Member.Name;

        // Handle Nullable<T> properties
        if (selector.Body is UnaryExpression u)
            body = u.Operand as MemberExpression;
        if (body == null)
            throw new ArgumentException("Expression is not a MemberExpression", nameof(selector));

        return body.Member.Name;
    }

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

/// <summary>
    /// Generates SQL with computed columns included when they're used in WHERE clause.
    /// This prevents "Invalid column name" errors when paging translator creates subqueries.
    /// </summary>
    private string GenerateSqlWithComputedColumns(IQueryable<T> query)
    {
        var entityType = typeof(T);
        var computedColumns = GetComputedColumnsForEntity(entityType);
        
        if (computedColumns.Length == 0)
        {
            // No computed columns, use standard method
            return query.ToSqlCommand($"[{this.IdColumn}], [Json]");
}

        // Generate base SQL and then apply paging with computed column support
        var baseSql = query.ToSqlCommand($"[{this.IdColumn}], [Json]");
        return baseSql; // Will be processed by paging translator
    }

    /// <summary>
    /// Applies paging with computed column support.
    /// </summary>
    private string ApplyPagingWithComputedColumns(string sql, int page, int size)
    {
        var entityType = typeof(T);
        var computedColumns = GetComputedColumnsForEntity(entityType);
        
        if (computedColumns.Length == 0)
        {
            // No computed columns, use standard paging
            return this.PagingTranslator.Translate(sql, page, size);
        }

        // Use paging translator with computed column support
        if (this.PagingTranslator is Sql2012PagingTranslator sqlTranslator)
        {
            return sqlTranslator.TranslateWithComputedColumns(sql, page, size, computedColumns);
        }

        // Fallback to standard paging
        return this.PagingTranslator.Translate(sql, page, size);
    }

    /// <summary>
    /// Gets the computed columns for an entity type based on known table schemas.
    /// This is a temporary solution until we can dynamically read schema metadata.
    /// </summary>
    private static string[] GetComputedColumnsForEntity(Type entityType)
    {
        var entityName = entityType.Name;

        return entityName switch
        {
            "TillSession" => new[]
            {
                "ShopId", "StaffUserName", "Status", "VerifiedByUserName", "ClosedByUserName",
                "IsForceClose", "IsLateClose", "OpenedAt", "ClosedAt", "VerifiedAt", "ExpectedCloseDate"
            },
            "Rental" => new[]
            {
                "RentedFromShopId", "RentedToShopId", "RenterId", "VehicleId", "Status",
                "StartDate", "EndDate", "ActualEndDate", "TotalAmount", "DepositAmount"
            },
            "Booking" => new[]
            {
                "ShopId", "RenterId", "VehicleId", "Status", "StartDate", "EndDate",
                "TotalAmount", "DepositAmount", "IsConfirmed"
            },
            "Vehicle" => new[]
            {
                "ShopId", "Status", "VehicleTypeId", "Make", "Model", "Year", "LicensePlate",
                "DailyRate", "IsAvailable", "LocationId"
            },
            "Renter" => new[]
            {
                "FirstName", "LastName", "Email", "Phone", "Country", "IdType",
                "IdNumber", "IsVerified", "DateOfBirth"
            },
            "Payment" => new[]
            {
                "RentalId", "RenterId", "PaymentMethod", "Status", "Amount",
                "PaymentDate", "Currency", "IsRefunded"
            },
            "Deposit" => new[]
            {
                "RentalId", "RenterId", "Status", "Amount", "DepositDate",
                "RefundDate", "Currency", "IsRefunded"
            },
            "Shop" => new[]
            {
                "OrganizationId", "Name", "Code", "Status", "City", "Province",
                "Address", "Phone", "Email", "IsOpen"
            },
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Gets a data reader for specific columns without loading full entities.
    /// Performance-optimized: no [Json] column, no deserialization.
    /// </summary>
    /// <param name="query">The query to execute</param>
    /// <param name="columns">Column names to select</param>
    /// <returns>IDataReader with CloseConnection behavior - disposes connection when reader is closed</returns>
    public async Task<IDataReader> GetReaderAsync(IQueryable<T> query, params string[] columns)
    {
        if (columns.Length == 0)
            throw new ArgumentException("At least one column must be specified", nameof(columns));

        var fields = string.Join(", ", columns.Select(c => $"[{c}]"));
        var sql = query.ToSqlCommand(fields);
        var schema = this.GetSchema();

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var conn = new SqlConnection(this.Context.GetConnectionString());
                var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                // CommandBehavior.CloseConnection ensures connection closes when reader is disposed
                return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            });

        if (await pr.FinalException.CreateSqlObjectIfMissingAsync(schema, this.TableName))
            return await this.GetReaderAsync(query, columns);

        if (pr.FinalException is not null)
        {
            System.Diagnostics.Debug.WriteLine($"SQL Error for {typeof(T).Name}: {sql}");
            Console.WriteLine($"SQL Error for {typeof(T).Name}: {sql}");
            throw new InvalidOperationException($"SQL Error for {typeof(T).Name}: {sql}", pr.FinalException);
        }

        return pr.Result;
    }
}
