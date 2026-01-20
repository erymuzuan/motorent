using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using Polly;

namespace MotoRent.Core.Repository;

public partial class CoreSqlJsonRepository<T>(
    IRequestContext context,
    ICorePagingTranslator translator,
    ICacheService cacheService,
    CoreSqlQueryProvider queryProvider,
    CoreRepositoryOptions options)
    : IRepository<T>
    where T : Entity, new()
{
    private IRequestContext Context { get; } = context;
    private ICorePagingTranslator Translator { get; } = translator;
    private ICacheService CacheService { get; } = cacheService;
    private CoreSqlQueryProvider QueryProvider { get; } = queryProvider;
    private readonly string m_connectionString = options.ConnectionString;

    public async Task<T?> LoadOneAsync(IQueryable<T> query)
    {
        var elementType = typeof(T);
        var sql = query.ToString()!.Replace("[Data]", $"[{elementType.Name}Id],[Json]");

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
            Console.WriteLine($"\ud83d\udc4d\t{elementType.Name}: {key}");
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
        Console.WriteLine(sql);
        var span = sql.AsSpan();
        const string CUT = "WHERE ";
        var index = span.IndexOf(CUT) + CUT.Length;
        return span.Slice(index, sql.Length - index).ToString();
    }

    private async Task<T?> LoadOneAsync2(string sql, string table)
    {
        Console.WriteLine($"\u2a37\t {table}");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var json = reader.GetString(1);
            var t = DeserializeFromJsonWithId(json, reader.GetInt32(0));
            return t;
        }

        return null;
    }

    public async Task<LoadOperation<T>> LoadAsync(IQueryable<T> query, int page = 1, int size = 40, bool includeTotalRows = false)
    {
        var elementType = typeof(T);

        var sql = new StringBuilder(query.ToString());
        var sqlCountRows = sql.ToString().Replace("[Data]", "COUNT(*)");
        sql.Replace("[Data]", $"[{elementType.Name}Id],[Json]");
        if (!sql.ToString().Contains("ORDER"))
        {
            sql.AppendLine();
            sql.AppendFormat("ORDER BY [{0}Id]", elementType.Name);
        }

        sql = new StringBuilder(Translator.Translate<T>(sql.ToString(), page, size));

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var list = new List<T>();
                await using var conn = new SqlConnection(m_connectionString);
                await using var cmd = new SqlCommand(sql.ToString(), conn);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var json = reader.GetString(1);
                    var t = DeserializeFromJsonWithId(json, reader.GetInt32(0));
                    if (t != null)
                        list.Add(t);
                }

                return list;
            });

        if (pr.FinalException is SqlException se)
            throw new Exception(sql.ToString(), se);

        if (null != pr.FinalException)
            throw pr.FinalException;

        var lo = new LoadOperation<T>
        {
            Page = page,
            PageSize = size,
        };
        lo.ItemCollection.AddRange(pr.Result ?? []);

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
        var now = DateTimeOffset.Now;
        var typeName = typeof(T).Name;

        await using var conn = new SqlConnection(m_connectionString);
        await conn.OpenAsync();

        var sql = $@"
            INSERT INTO [Core].[{typeName}] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
            OUTPUT INSERTED.[{typeName}Id]
            VALUES (@Json, @Username, @Username, @Timestamp, @Timestamp)";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Json", json);
        cmd.Parameters.AddWithValue("@Username", username);
        cmd.Parameters.AddWithValue("@Timestamp", now);

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () =>
            {
                var id = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                entity.SetId(id);
                entity.CreatedBy = username;
                entity.ChangedBy = username;
                entity.CreatedTimestamp = now;
                entity.ChangedTimestamp = now;
                return id;
            });

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<int> UpdateAsync(T entity, string username)
    {
        var json = entity.ToJsonString();
        var now = DateTimeOffset.Now;
        var typeName = typeof(T).Name;

        await using var conn = new SqlConnection(m_connectionString);
        await conn.OpenAsync();

        var sql = $@"
            UPDATE [Core].[{typeName}]
            SET [Json] = @Json, [ChangedBy] = @Username, [ChangedTimestamp] = @Timestamp
            WHERE [{typeName}Id] = @Id";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Json", json);
        cmd.Parameters.AddWithValue("@Username", username);
        cmd.Parameters.AddWithValue("@Timestamp", now);
        cmd.Parameters.AddWithValue("@Id", entity.GetId());

        entity.ChangedBy = username;
        entity.ChangedTimestamp = now;

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () => await cmd.ExecuteNonQueryAsync());

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<int> DeleteAsync(T entity)
    {
        var typeName = typeof(T).Name;

        await using var conn = new SqlConnection(m_connectionString);
        await conn.OpenAsync();

        var sql = $"DELETE FROM [Core].[{typeName}] WHERE [{typeName}Id] = @Id";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", entity.GetId());

        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () => await cmd.ExecuteNonQueryAsync());

        if (pr.FinalException != null)
            throw pr.FinalException;

        return pr.Result;
    }

    public async Task<int> DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var query = new CoreQuery<T>(QueryProvider).Where(predicate);

        var sql = query.ToString()!.Replace("SELECT [Data]", "DELETE ").Replace("SELECT  [Data]", "DELETE ");
        await using var conn = new SqlConnection(m_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        var pr = await Policy.Handle<SqlException>(HasNetworkError)
            .WaitAndRetryAsync(5, Sleep)
            .ExecuteAndCaptureAsync(async () => await cmd.ExecuteNonQueryAsync());

        if (null != pr.FinalException)
            throw pr.FinalException;

        return pr.Result;
    }

    private static TimeSpan Sleep(int c) => TimeSpan.FromMilliseconds(600 * Math.Pow(2, c));

    private static bool HasNetworkError(SqlException exception) => exception switch
    {
        null => false,
        { Number: 40613 } => true,
        { Message.Length: > 0 } when exception.Message.Contains("deadlocked") => true,
        { Message.Length: > 0 } when exception.Message.Contains("timeout") => true,
        { Message.Length: > 0 } when exception.Message.Contains("Please retry the connection later") => true,
        _ => false
    };

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
