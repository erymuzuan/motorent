using System.Text;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;
using Npgsql;
using NpgsqlTypes;
using Polly;
using Polly.Retry;

namespace MotoRent.PostgreSqlRepository;

/// <summary>
/// PostgreSQL batch transactional persistence for tenant-scoped entities.
/// All inserts/updates/deletes execute in a single database transaction.
/// Uses RLS for tenant isolation via DbConnectionInterceptor.
/// </summary>
public class PgPersistence(
    IRequestContext context,
    IPgMetadata metadata,
    DbConnectionInterceptor interceptor,
    IMessageBroker? messageBroker = null) : IPersistence
{
    private const int MAX_ITEMS_PER_BATCH = 250;

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

    public async Task<SubmitOperation> SubmitChanges(Entity item)
    {
        var session = new PersistenceSession(null!, context.GetUserName() ?? "system");
        return await SubmitChanges([item], [], session);
    }

    public async Task<SubmitOperation> SubmitChanges(
        IEnumerable<Entity> addedOrUpdatedItems,
        IEnumerable<Entity> deletedItems,
        PersistenceSession session)
    {
        var itemList = addedOrUpdatedItems.ToList();
        var deleteList = deletedItems.ToList();

        if (itemList.Count == 0 && deleteList.Count == 0)
            return SubmitOperation.CreateSuccess();

        if (itemList.Count + deleteList.Count > MAX_ITEMS_PER_BATCH)
            return SubmitOperation.CreateFailure($"Batch exceeds {MAX_ITEMS_PER_BATCH} items");

        var username = session.Username;
        var now = DateTimeOffset.UtcNow;
        int inserted = 0, updated = 0, deleted = 0;

        try
        {
            var connectionString = context.GetConnectionString();
            var pipeline = CreateRetryPipeline();

            var books = await pipeline.ExecuteAsync(async ct =>
            {
                var result = new Dictionary<string, int>();
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync(ct);
                await interceptor.SetTenantAsync(conn);

                await using var transaction = await conn.BeginTransactionAsync(ct);
                try
                {
                    // Process deletes first
                    foreach (var entity in deleteList)
                    {
                        var tableName = entity.GetType().Name;
                        var id = entity.GetId();
                        var sql = $"DELETE FROM \"{tableName}\" WHERE \"{tableName}Id\" = @Id";
                        await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                        cmd.Parameters.AddWithValue("@Id", id);
                        await cmd.ExecuteNonQueryAsync(ct);
                        deleted++;
                    }

                    // Process inserts and updates
                    foreach (var entity in itemList)
                    {
                        var tableName = entity.GetType().Name;
                        var table = metadata.GetTable(tableName);

                        if (string.IsNullOrWhiteSpace(entity.WebId))
                            entity.WebId = Guid.NewGuid().ToString();

                        var json = entity.ToJsonString();

                        if (entity.GetId() == 0)
                        {
                            // INSERT with RETURNING to capture identity
                            var columns = table.Columns.Where(c => !c.IsIdentity && c.CanWrite).ToArray();
                            var columnNames = string.Join(",", columns.Select(c => $"\"{c.Name}\""));
                            var paramNames = string.Join(",", columns.Select((c, i) => $"@p{i}"));

                            var sql = $"""
                                INSERT INTO "{tableName}"
                                ({columnNames},"tenant_id")
                                VALUES ({paramNames},current_setting('app.current_tenant'))
                                RETURNING '{entity.WebId}' AS "WebId", "{tableName}Id" AS "Id"
                                """;

                            await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                            for (var i = 0; i < columns.Length; i++)
                            {
                                var col = columns[i];
                                var value = GetParameterValue(col, entity, json, username, now, true);
                                AddParameter(cmd, col, i, value);
                            }

                            await using var reader = await cmd.ExecuteReaderAsync(ct);
                            if (await reader.ReadAsync(ct))
                            {
                                result[reader.GetString(0)] = reader.GetInt32(1);
                            }
                            inserted++;
                        }
                        else
                        {
                            // UPDATE
                            var columns = table.Columns
                                .Where(c => !c.IsIdentity && c.CanWrite)
                                .Where(c => c.Name != "CreatedBy" && c.Name != "CreatedTimestamp")
                                .ToArray();

                            var setClause = string.Join(",", columns.Select((c, i) => $"\"{c.Name}\"=@p{i}"));
                            var sql = $"""
                                UPDATE "{tableName}"
                                SET {setClause}
                                WHERE "{tableName}Id" = @Id
                                """;

                            await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                            for (var i = 0; i < columns.Length; i++)
                            {
                                var col = columns[i];
                                var value = GetParameterValue(col, entity, json, username, now, false);
                                AddParameter(cmd, col, i, value);
                            }
                            cmd.Parameters.AddWithValue("@Id", entity.GetId());
                            await cmd.ExecuteNonQueryAsync(ct);
                            updated++;
                        }
                    }

                    await transaction.CommitAsync(ct);
                    return result;
                }
                catch
                {
                    try { await transaction.RollbackAsync(ct); } catch { /* already disposed */ }
                    throw;
                }
            });

            // Update entity IDs from books for newly inserted entities
            foreach (var entity in itemList.Where(e => e.GetId() == 0))
            {
                if (books.TryGetValue(entity.WebId!, out var newId))
                {
                    entity.SetId(newId);
                    entity.CreatedBy = username;
                    entity.CreatedTimestamp = now;
                }
                entity.ChangedBy = username;
                entity.ChangedTimestamp = now;
            }

            // Update audit fields for updated entities
            foreach (var entity in itemList.Where(e => e.GetId() > 0))
            {
                entity.ChangedBy = username;
                entity.ChangedTimestamp = now;
            }

            // Publish messages if broker configured
            if (messageBroker != null)
                await PublishMessagesAsync(itemList, deleteList, session.Operation ?? "", username);

            return SubmitOperation.CreateSuccess(inserted, updated, deleted, books);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[PgPersistence] Submit FAILED: {ex.Message}");
            Console.Error.WriteLine($"[PgPersistence] {ex}");
            return SubmitOperation.CreateFailure($"Submit failed: {ex.Message}", ex);
        }
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
        var type = entity.GetType();
        var prop = type.GetProperty(column.Name);
        if (prop is null)
            return DBNull.Value;

        var value = prop.GetValue(entity);
        if (value is null)
            return column.IsNullable ? DBNull.Value : DBNull.Value;

        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        return (propType.Name, value!) switch
        {
            // Thai Buddhist calendar handling
            ("DateTimeOffset", DateTimeOffset { Year: > 3000 } ut) => ut.AddYears(-1086).ToUniversalTime(),
            ("DateTimeOffset", DateTimeOffset { Year: > 2500 } ut) => ut.AddYears(-543).ToUniversalTime(),
            // PostgreSQL requires UTC offset for timestamptz
            ("DateTimeOffset", DateTimeOffset dto) => dto.ToUniversalTime(),
            ("DateOnly", DateOnly { Year: > 3000 } ut) => $"{ut.Year - 1086}-{ut:MM-dd}",
            ("DateOnly", DateOnly { Year: > 2500 } ut) => $"{ut.Year - 543}-{ut:MM-dd}",
            ("DateTime", DateTime { Year: 1 }) when column.IsNullable => DBNull.Value,
            ("DateOnly", DateOnly { Year: 1 }) when column.IsNullable => DBNull.Value,
            (_, DateOnly { Year: > 1920 and < 2120 } dt) => $"{dt:yyyy-MM-dd}",
            _ when propType.IsEnum => value!.ToString()!,
            _ when propType.IsGenericType && propType.GenericTypeArguments[0].IsEnum => value!.ToString()!,
            _ => value
        };
    }

    private static void AddParameter(NpgsqlCommand cmd, PgColumn column, int index, object parameterValue)
    {
        if (column.SqlType == "jsonb")
            cmd.Parameters.Add(new NpgsqlParameter($"@p{index}", NpgsqlDbType.Jsonb) { Value = parameterValue });
        else if (column.SqlType == "date" && parameterValue is DateTimeOffset dto)
            cmd.Parameters.Add(new NpgsqlParameter($"@p{index}", NpgsqlDbType.Date) { Value = DateOnly.FromDateTime(dto.DateTime) });
        else if (column.SqlType == "date" && parameterValue is string dateStr)
            cmd.Parameters.Add(new NpgsqlParameter($"@p{index}", NpgsqlDbType.Date) { Value = DateOnly.Parse(dateStr) });
        else
            cmd.Parameters.AddWithValue($"@p{index}", parameterValue);
    }

    private async Task PublishMessagesAsync(List<Entity> itemList, List<Entity> deleteList,
        string operation, string username)
    {
        if (messageBroker == null) return;

        try
        {
            foreach (var entity in itemList)
            {
                var crud = entity.CreatedTimestamp == entity.ChangedTimestamp
                    ? CrudOperation.Added
                    : CrudOperation.Changed;

                var message = new BrokeredMessage(entity)
                {
                    Entity = entity.GetType().Name,
                    EntityId = entity.GetId(),
                    Crud = crud,
                    Operation = operation,
                    Username = username,
                    AccountNo = context.GetAccountNo(),
                    Id = Guid.NewGuid().ToString("N")
                };

                await messageBroker.SendAsync(message);
            }

            foreach (var entity in deleteList)
            {
                var message = new BrokeredMessage(entity)
                {
                    Entity = entity.GetType().Name,
                    EntityId = entity.GetId(),
                    Crud = CrudOperation.Deleted,
                    Operation = operation,
                    Username = username,
                    AccountNo = context.GetAccountNo(),
                    Id = Guid.NewGuid().ToString("N")
                };

                await messageBroker.SendAsync(message);
            }
        }
        catch (Exception)
        {
            // Log but don't fail the operation if message publishing fails
            // The entities are already saved, messaging is async notification
        }
    }
}
