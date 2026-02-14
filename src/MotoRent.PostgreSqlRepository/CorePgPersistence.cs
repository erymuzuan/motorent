using System.Text;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using Npgsql;
using Polly;
using Polly.Retry;

namespace MotoRent.PostgreSqlRepository;

/// <summary>
/// PostgreSQL batch transactional persistence for Core schema entities.
/// Does NOT use RLS/tenant interception - Core entities are shared across tenants.
/// </summary>
public class CorePgPersistence(IRequestContext context) : IPersistence
{
    private const int MAX_ITEMS_PER_BATCH = 250;
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
        { SqlState: "40P01" } => true,
        { SqlState: "57P03" } => true,
        { SqlState: "08006" } => true,
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
            var pipeline = CreateRetryPipeline();

            var books = await pipeline.ExecuteAsync(async ct =>
            {
                var result = new Dictionary<string, int>();
                await using var conn = new NpgsqlConnection(m_connectionString);
                await conn.OpenAsync(ct);

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
                        var typeName = entity.GetType().Name;

                        if (string.IsNullOrWhiteSpace(entity.WebId))
                            entity.WebId = Guid.NewGuid().ToString();

                        var json = entity.ToJsonString();

                        if (entity.GetId() == 0)
                        {
                            // INSERT with RETURNING
                            var sql = $"""
                                INSERT INTO "{typeName}" ("Json", "CreatedBy", "ChangedBy", "CreatedTimestamp", "ChangedTimestamp")
                                VALUES (@Json, @Username, @Username, @Timestamp, @Timestamp)
                                RETURNING '{entity.WebId}' AS "WebId", "{typeName}Id" AS "Id"
                                """;

                            await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                            cmd.Parameters.AddWithValue("@Json", json);
                            cmd.Parameters.AddWithValue("@Username", username);
                            cmd.Parameters.AddWithValue("@Timestamp", now);

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
                            var sql = $"""
                                UPDATE "{typeName}"
                                SET "Json" = @Json, "ChangedBy" = @Username, "ChangedTimestamp" = @Timestamp
                                WHERE "{typeName}Id" = @Id
                                """;

                            await using var cmd = new NpgsqlCommand(sql, conn, transaction);
                            cmd.Parameters.AddWithValue("@Json", json);
                            cmd.Parameters.AddWithValue("@Username", username);
                            cmd.Parameters.AddWithValue("@Timestamp", now);
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
                    await transaction.RollbackAsync(ct);
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

            return SubmitOperation.CreateSuccess(inserted, updated, deleted, books);
        }
        catch (Exception ex)
        {
            return SubmitOperation.CreateFailure($"Submit failed: {ex.Message}", ex);
        }
    }
}
