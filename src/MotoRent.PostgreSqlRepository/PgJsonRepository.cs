using System.Data;
using System.Linq.Expressions;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.QueryProviders;
using Npgsql;
using NpgsqlTypes;
using Polly;
using Polly.Retry;

namespace MotoRent.PostgreSqlRepository;

/// <summary>
/// PostgreSQL JSON Repository implementation with Polly retry policies for transient failure resilience.
/// Uses RLS (Row Level Security) for tenant isolation via DbConnectionInterceptor.
/// </summary>
public partial class PgJsonRepository<T>(
    IRequestContext context,
    PgPagingTranslator pagingTranslator,
    DbConnectionInterceptor interceptor,
    IPgMetadata metadata) : IRepository<T> where T : Entity, new()
{
    private IRequestContext Context { get; } = context;
    private PgPagingTranslator PagingTranslator { get; } = pagingTranslator;
    private DbConnectionInterceptor Interceptor { get; } = interceptor;
    private IPgMetadata Metadata { get; } = metadata;
    private string IdColumn { get; } = $"{typeof(T).Name}Id";
    private string TableName { get; } = typeof(T).Name;

    private const string AuditColumns = "\"CreatedBy\", \"CreatedTimestamp\", \"ChangedBy\", \"ChangedTimestamp\"";

    private ResiliencePipeline CreateRetryPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(600),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(IsTransient)
            })
            .Build();
    }

    private static bool IsTransient(NpgsqlException exception) => exception switch
    {
        null => false,
        { SqlState: "40P01" } => true, // deadlock_detected
        { SqlState: "57P03" } => true, // cannot_connect_now
        { SqlState: "08006" } => true, // connection_failure
        { Message.Length: > 0 } when exception.Message.Contains("timeout") => true,
        _ => false
    };

    public IQueryable<T> CreateQuery()
    {
        var provider = ObjectBuilder.GetObject<QueryProvider>();
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
        var elementType = typeof(T);
        var sql = new System.Text.StringBuilder(query.ToString());
        var sqlCountRows = sql.ToString().Replace("\"Data\"", "COUNT(*)");
        sql.Replace("\"Data\"", $"\"{elementType.Name}Id\",\"Json\",{AuditColumns}");
        if (!sql.ToString().Contains("ORDER"))
        {
            sql.AppendLine();
            sql.AppendFormat("ORDER BY \"{0}Id\"", elementType.Name);
        }

        sql = new System.Text.StringBuilder(this.PagingTranslator.Translate(sql.ToString(), page, size));

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        var list = await pipeline.ExecuteAsync(async ct =>
        {
            var items = new List<T>();
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            await using var cmd = new NpgsqlCommand(sql.ToString(), conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var entity = this.MapFromReader(reader);
                if (entity is not null)
                    items.Add(entity);
            }

            return items;
        });

        var lo = new LoadOperation<T>
        {
            Page = page,
            PageSize = size,
        };
        lo.ItemCollection.AddRange(list);

        if (!includeTotalRows) return lo;
        var order = sqlCountRows.IndexOf("ORDER", StringComparison.Ordinal);
        var count = order == -1 ? sqlCountRows : sqlCountRows[..order];
        lo.TotalRows = await this.GetCountAsync(count);

        return lo;
    }

    public async Task<int> InsertAsync(T entity, string username)
    {
        if (string.IsNullOrWhiteSpace(entity.WebId))
            entity.WebId = Guid.NewGuid().ToString();

        var table = this.Metadata.GetTable(this.TableName);
        var columns = table.Columns.Where(c => !c.IsIdentity && c.CanWrite).ToArray();
        var json = entity.ToJsonString();
        var now = DateTimeOffset.UtcNow;

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        var newId = await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);

            var columnNames = string.Join(",", columns.Select(c => $"\"{c.Name}\""));
            var paramNames = string.Join(",", columns.Select((c, i) => $"@p{i}"));

            var sql = $"""
                INSERT INTO "{this.TableName}"
                ({columnNames},"tenant_id")
                VALUES ({paramNames},current_setting('app.current_tenant'))
                RETURNING "{this.IdColumn}"
                """;

            await using var cmd = new NpgsqlCommand(sql, conn);

            for (var i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                var value = GetParameterValue(col, entity, json, username, now, true);
                AddParameter(cmd, col, i, value);
            }

            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result);
        });

        entity.SetId(newId);
        entity.CreatedBy = username;
        entity.ChangedBy = username;
        entity.CreatedTimestamp = now;
        entity.ChangedTimestamp = now;

        return newId;
    }

    public async Task<int> UpdateAsync(T entity, string username)
    {
        var table = this.Metadata.GetTable(this.TableName);
        var columns = table.Columns
            .Where(c => !c.IsIdentity && c.CanWrite)
            .Where(c => c.Name != "CreatedBy" && c.Name != "CreatedTimestamp")
            .ToArray();

        var json = entity.ToJsonString();
        var now = DateTimeOffset.UtcNow;
        var id = entity.GetId();

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        var rowsAffected = await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);

            var setClause = string.Join(",", columns.Select((c, i) => $"\"{c.Name}\"=@p{i}"));
            var sql = $"""
                UPDATE "{this.TableName}"
                SET {setClause}
                WHERE "{this.IdColumn}" = @Id
                """;

            await using var cmd = new NpgsqlCommand(sql, conn);

            for (var i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                var value = GetParameterValue(col, entity, json, username, now, false);
                AddParameter(cmd, col, i, value);
            }
            cmd.Parameters.AddWithValue("@Id", id);

            return await cmd.ExecuteNonQueryAsync(ct);
        });

        entity.ChangedBy = username;
        entity.ChangedTimestamp = now;

        return rowsAffected;
    }

    public async Task<int> DeleteAsync(T entity)
    {
        var id = entity.GetId();

        var connectionString = this.Context.GetConnectionString();
        var pipeline = this.CreateRetryPipeline();
        var rowsAffected = await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);

            var sql = $"DELETE FROM \"{this.TableName}\" WHERE \"{this.IdColumn}\" = @Id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            return await cmd.ExecuteNonQueryAsync(ct);
        });

        return rowsAffected;
    }

    public async Task<int> DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var provider = ObjectBuilder.GetObject<QueryProvider>();
        var query = new Query<T>(provider).Where(predicate);

        var sql = query.ToString()!.Replace("SELECT \"Data\"", "DELETE ").Replace("SELECT  \"Data\"", "DELETE ");
        var connectionString = this.Context.GetConnectionString();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await this.Interceptor.SetTenantAsync(conn);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var pipeline = this.CreateRetryPipeline();
        var result = await pipeline.ExecuteAsync(async ct => await cmd.ExecuteNonQueryAsync(ct));
        return result;
    }

    private T? MapFromReader(NpgsqlDataReader reader)
    {
        var json = reader.GetString(reader.GetOrdinal("Json"));
        var entity = json.DeserializeFromJson<T>();

        if (entity is not null)
        {
            entity.SetId(reader.GetInt32(reader.GetOrdinal(this.IdColumn)));

            // Read audit columns
            var createdByOrdinal = reader.GetOrdinal("CreatedBy");
            if (!reader.IsDBNull(createdByOrdinal))
                entity.CreatedBy = reader.GetString(createdByOrdinal);

            var createdTimestampOrdinal = reader.GetOrdinal("CreatedTimestamp");
            if (!reader.IsDBNull(createdTimestampOrdinal))
                entity.CreatedTimestamp = reader.GetFieldValue<DateTimeOffset>(createdTimestampOrdinal);

            var changedByOrdinal = reader.GetOrdinal("ChangedBy");
            if (!reader.IsDBNull(changedByOrdinal))
                entity.ChangedBy = reader.GetString(changedByOrdinal);

            var changedTimestampOrdinal = reader.GetOrdinal("ChangedTimestamp");
            if (!reader.IsDBNull(changedTimestampOrdinal))
                entity.ChangedTimestamp = reader.GetFieldValue<DateTimeOffset>(changedTimestampOrdinal);
        }

        return entity;
    }

    private static object GetParameterValue(PgColumn col, Entity entity, string json, string username, DateTimeOffset now, bool isInsert) => col.Name switch
    {
        "Json" => json,
        "CreatedTimestamp" when isInsert => now,
        "CreatedBy" when isInsert => username,
        "ChangedTimestamp" => now,
        "ChangedBy" => username,
        _ => GetEntityPropertyValue(col, entity)
    };

    private static object GetEntityPropertyValue(PgColumn column, Entity entity)
    {
        var prop = entity.GetType().GetProperty(column.Name);
        if (prop is null)
            return DBNull.Value;

        var value = prop.GetValue(entity);
        if (value is null)
            return column.IsNullable ? DBNull.Value : DBNull.Value;

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
            _ when type.IsEnum => value!.ToString()!,
            _ when type.IsGenericType && type.GenericTypeArguments[0].IsEnum => value!.ToString()!,
            _ => value
        };
    }

    private static void AddParameter(NpgsqlCommand cmd, PgColumn column, int index, object parameterValue)
    {
        if (column.SqlType == "jsonb")
            cmd.Parameters.Add(new NpgsqlParameter($"@p{index}", NpgsqlDbType.Jsonb) { Value = parameterValue });
        else
            cmd.Parameters.AddWithValue($"@p{index}", parameterValue);
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

        if (underlyingType.IsEnum)
        {
            if (Enum.TryParse(underlyingType, value.ToString(), out var enumValue))
                return (TTarget)enumValue!;
            return default;
        }

        if (underlyingType == typeof(DateOnly) && value is DateTime dt)
            return (TTarget)(object)DateOnly.FromDateTime(dt);

        if (underlyingType == typeof(TimeOnly) && value is TimeSpan ts)
            return (TTarget)(object)TimeOnly.FromTimeSpan(ts);

        try
        {
            return (TTarget)Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            return default;
        }
    }

    public async Task<IDataReader> GetReaderAsync(IQueryable<T> query, params string[] columns)
    {
        if (columns.Length == 0)
            throw new ArgumentException("At least one column must be specified", nameof(columns));

        var fields = string.Join(", ", columns.Select(c => $"\"{c}\""));
        var sql = query.ToString()!.Replace("\"Data\"", fields);
        var connectionString = this.Context.GetConnectionString();

        var pipeline = this.CreateRetryPipeline();
        var reader = await pipeline.ExecuteAsync(async ct =>
        {
            var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await this.Interceptor.SetTenantAsync(conn);
            var cmd = new NpgsqlCommand(sql, conn);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, ct);
        });

        return reader;
    }
}
