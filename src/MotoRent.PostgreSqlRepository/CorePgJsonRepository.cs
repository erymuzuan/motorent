using System.Data;
using System.Linq.Expressions;
using System.Text;
using MotoRent.Core.Repository;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using Npgsql;
using Polly;
using Polly.Retry;

namespace MotoRent.PostgreSqlRepository;

/// <summary>
/// PostgreSQL JSON Repository for Core schema entities (Organization, User, Setting, etc.).
/// Does NOT use RLS/tenant interception - Core entities are shared across tenants.
/// Supports HybridCache for Organization and User entities.
/// </summary>
public partial class CorePgJsonRepository<T>(
    IRequestContext context,
    ICorePagingTranslator translator,
    ICacheService cacheService,
    CorePgQueryProvider queryProvider)
    : IRepository<T>
    where T : Entity, new()
{
    private IRequestContext Context { get; } = context;
    private ICorePagingTranslator Translator { get; } = translator;
    private ICacheService CacheService { get; } = cacheService;
    private CorePgQueryProvider QueryProvider { get; } = queryProvider;
    private readonly string m_connectionString = MotoConfig.ConnectionString;

    private static ResiliencePipeline CreateRetryPipeline()
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
        return new CoreQuery<T>(QueryProvider);
    }

    public async Task<T?> LoadOneAsync(IQueryable<T> query)
    {
        var elementType = typeof(T);
        var sql = query.ToString()!.Replace("\"Data\"", $"\"{elementType.Name}Id\",\"Json\",\"CreatedBy\",\"ChangedBy\",\"CreatedTimestamp\",\"ChangedTimestamp\"");

        if (elementType.Name is "Organization" or "User")
        {
            var account = await this.Context.GetAccountNoAsync();
            var key = $"Core{elementType.Name}{GetCacheKey(sql)}";
            var item = await this.CacheService.GetOrCreateAsync(key,
                async _ => await this.LoadOneAsync2(sql, elementType.Name),
                30 * 60,
                1 * 60,
                [account ?? "Core", "Core", elementType.Name]
            );
            if (item is not null)
                return item;
        }

        return await this.LoadOneAsync2(sql, elementType.Name);
    }

    public async Task<T?> LoadOneAsync(Expression<Func<T, bool>> predicate)
    {
        var query = new CoreQuery<T>(QueryProvider).Where(predicate);
        return await LoadOneAsync(query);
    }

    private static string GetCacheKey(string sql)
    {
        var span = sql.AsSpan();
        const string CUT = "WHERE ";
        var index = span.IndexOf(CUT) + CUT.Length;
        if (index < CUT.Length) return sql;
        return span.Slice(index, sql.Length - index).ToString();
    }

    private async Task<T?> LoadOneAsync2(string sql, string table)
    {
        await using var conn = new NpgsqlConnection(m_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader.GetString(1);
            var t = DeserializeFromJsonWithId(json, reader.GetInt32(0));
            if (t != null)
            {
                t.CreatedBy = reader.GetString(2);
                t.ChangedBy = reader.GetString(3);
                t.CreatedTimestamp = reader.GetFieldValue<DateTimeOffset>(4);
                t.ChangedTimestamp = reader.GetFieldValue<DateTimeOffset>(5);
            }
            return t;
        }

        return null;
    }

    public async Task<LoadOperation<T>> LoadAsync(IQueryable<T> query, int page = 1, int size = 40, bool includeTotalRows = false)
    {
        var elementType = typeof(T);

        var sql = new StringBuilder(query.ToString());
        var sqlCountRows = sql.ToString().Replace("\"Data\"", "COUNT(*)");
        sql.Replace("\"Data\"", $"\"{elementType.Name}Id\",\"Json\",\"CreatedBy\",\"ChangedBy\",\"CreatedTimestamp\",\"ChangedTimestamp\"");
        if (!sql.ToString().Contains("ORDER"))
        {
            sql.AppendLine();
            sql.AppendFormat("ORDER BY \"{0}Id\"", elementType.Name);
        }

        sql = new StringBuilder(Translator.Translate<T>(sql.ToString(), page, size));

        var pipeline = CreateRetryPipeline();
        var list = await pipeline.ExecuteAsync(async ct =>
        {
            var items = new List<T>();
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql.ToString(), conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var json = reader.GetString(1);
                var t = DeserializeFromJsonWithId(json, reader.GetInt32(0));
                if (t != null)
                {
                    t.CreatedBy = reader.GetString(2);
                    t.ChangedBy = reader.GetString(3);
                    t.CreatedTimestamp = reader.GetFieldValue<DateTimeOffset>(4);
                    t.ChangedTimestamp = reader.GetFieldValue<DateTimeOffset>(5);
                    items.Add(t);
                }
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
        lo.TotalRows = await GetCountAsync(count);

        return lo;
    }

    public async Task<int> InsertAsync(T entity, string username)
    {
        if (string.IsNullOrWhiteSpace(entity.WebId))
            entity.WebId = Guid.NewGuid().ToString();

        var json = entity.ToJsonString();
        var now = DateTimeOffset.UtcNow;
        var typeName = typeof(T).Name;

        var pipeline = CreateRetryPipeline();
        var newId = await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);

            var sql = $"""
                INSERT INTO "{typeName}" ("Json", "CreatedBy", "ChangedBy", "CreatedTimestamp", "ChangedTimestamp")
                VALUES (@Json, @Username, @Username, @Timestamp, @Timestamp)
                RETURNING "{typeName}Id"
                """;

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Json", json);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Timestamp", now);

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
        var json = entity.ToJsonString();
        var now = DateTimeOffset.UtcNow;
        var typeName = typeof(T).Name;

        var pipeline = CreateRetryPipeline();
        var rowsAffected = await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);

            var sql = $"""
                UPDATE "{typeName}"
                SET "Json" = @Json, "ChangedBy" = @Username, "ChangedTimestamp" = @Timestamp
                WHERE "{typeName}Id" = @Id
                """;

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Json", json);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Timestamp", now);
            cmd.Parameters.AddWithValue("@Id", entity.GetId());

            return await cmd.ExecuteNonQueryAsync(ct);
        });

        entity.ChangedBy = username;
        entity.ChangedTimestamp = now;

        return rowsAffected;
    }

    public async Task<int> DeleteAsync(T entity)
    {
        var typeName = typeof(T).Name;

        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);

            var sql = $"DELETE FROM \"{typeName}\" WHERE \"{typeName}Id\" = @Id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", entity.GetId());

            return await cmd.ExecuteNonQueryAsync(ct);
        });
    }

    public async Task<int> DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var query = new CoreQuery<T>(QueryProvider).Where(predicate);

        var sql = query.ToString()!.Replace("SELECT \"Data\"", "DELETE ").Replace("SELECT  \"Data\"", "DELETE ");
        var pipeline = CreateRetryPipeline();
        return await pipeline.ExecuteAsync(async ct =>
        {
            await using var conn = new NpgsqlConnection(m_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync(ct);
        });
    }

    private static T? DeserializeFromJsonWithId(string json, int id)
    {
        var entity = json.DeserializeFromJson<T>();
        if (entity != null)
        {
            entity.SetId(id);
        }
        return entity;
    }

    private string GetMemberName<TResult>(Expression<Func<T, TResult>> selector)
    {
        if (selector is not LambdaExpression me) return string.Empty;
        var body = me.Body as MemberExpression;
        if (body != null) return body.Member.Name;

        if (selector.Body is UnaryExpression ub)
            body = ub.Operand as MemberExpression;
        if (body == null)
            throw new ArgumentException("Expression is not a MemberExpression", nameof(selector));

        return body.Member.Name;
    }
}
