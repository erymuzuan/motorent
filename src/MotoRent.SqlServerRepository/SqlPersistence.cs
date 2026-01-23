using System.Text;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;
using Polly;

namespace MotoRent.SqlServerRepository;

/// <summary>
/// Batch transactional persistence for entities.
/// All inserts/updates/deletes execute in a single database transaction.
/// </summary>
public class SqlPersistence(
    IRequestContext context,
    ISqlServerMetadata metadata,
    IMessageBroker? messageBroker = null) : IPersistence
{
    private const int MAX_ITEMS_PER_BATCH = 250;

    private static bool HasNetworkError(SqlException exception) => exception switch
    {
        null => false,
        { Number: 40613 } => true,
        { Message.Length: > 0 } when exception.Message.Contains("deadlocked") => true,
        { Message.Length: > 0 } when exception.Message.Contains("timeout") => true,
        { Message.Length: > 0 } when exception.Message.Contains("Please retry the connection later") => true,
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

        var schema = context.GetSchema() ?? "MotoRent";
        var username = session.Username;
        var now = DateTimeOffset.Now;
        int inserted = 0, updated = 0, deleted = 0;

        try
        {
            var sql = new StringBuilder();
            var parameters = new List<SqlParameter>();
            int paramIndex = 0;

            sql.AppendLine("SET NOCOUNT ON;");
            sql.AppendLine("BEGIN TRANSACTION;");
            sql.AppendLine("BEGIN TRY");

            // Process deletes first
            foreach (var entity in deleteList)
            {
                var tableName = entity.GetType().Name;
                var id = entity.GetId();
                sql.AppendLine($"DELETE FROM [{schema}].[{tableName}] WHERE [{tableName}Id] = @p{paramIndex};");
                parameters.Add(new SqlParameter($"@p{paramIndex++}", id));
                deleted++;
            }

            // Process inserts and updates
            foreach (var entity in itemList)
            {
                var tableName = entity.GetType().Name;
                var table = await metadata.GetTableAsync(schema, tableName);
                if (table == null)
                    throw new InvalidOperationException($"Table [{schema}].[{tableName}] not found");

                if (string.IsNullOrWhiteSpace(entity.WebId))
                    entity.WebId = Guid.NewGuid().ToString();

                var json = entity.ToJsonString();

                if (entity.GetId() == 0)
                {
                    // INSERT with OUTPUT to capture identity
                    var columns = table.Columns.Where(c => !c.IsIdentity && c.CanWrite).ToArray();
                    var columnNames = string.Join(",", columns.Select(c => $"[{c.Name}]"));
                    var paramNames = new List<string>();

                    foreach (var col in columns)
                    {
                        var value = GetParameterValue(col, entity, json, username, now, true);
                        paramNames.Add($"@p{paramIndex}");
                        parameters.Add(new SqlParameter($"@p{paramIndex++}", value ?? DBNull.Value));
                    }

                    sql.AppendLine($"INSERT INTO [{schema}].[{tableName}] ({columnNames})");
                    sql.AppendLine($"OUTPUT '{entity.WebId}' AS WebId, INSERTED.[{tableName}Id] AS Id");
                    sql.AppendLine($"VALUES ({string.Join(",", paramNames)});");
                    inserted++;
                }
                else
                {
                    // UPDATE
                    var columns = table.Columns
                        .Where(c => !c.IsIdentity && c.CanWrite)
                        .Where(c => c.Name != "CreatedBy" && c.Name != "CreatedDate" && c.Name != "CreatedTimestamp")
                        .ToArray();

                    var setClauses = new List<string>();
                    foreach (var col in columns)
                    {
                        var value = GetParameterValue(col, entity, json, username, now, false);
                        setClauses.Add($"[{col.Name}]=@p{paramIndex}");
                        parameters.Add(new SqlParameter($"@p{paramIndex++}", value ?? DBNull.Value));
                    }

                    sql.AppendLine($"UPDATE [{schema}].[{tableName}]");
                    sql.AppendLine($"SET {string.Join(",", setClauses)}");
                    sql.AppendLine($"WHERE [{tableName}Id] = @p{paramIndex};");
                    parameters.Add(new SqlParameter($"@p{paramIndex++}", entity.GetId()));
                    updated++;
                }
            }

            sql.AppendLine("COMMIT TRANSACTION;");
            sql.AppendLine("END TRY");
            sql.AppendLine("BEGIN CATCH");
            sql.AppendLine("ROLLBACK TRANSACTION;");
            sql.AppendLine("THROW;");
            sql.AppendLine("END CATCH");

            // Execute with Polly retry
            var pr = await Policy.Handle<SqlException>(HasNetworkError)
                .WaitAndRetryAsync(5, c => TimeSpan.FromMilliseconds(600 * Math.Pow(2, c)))
                .ExecuteAndCaptureAsync(async () =>
                {
                    var books = new Dictionary<string, int>();
                    await using var conn = new SqlConnection(context.GetConnectionString());
                    await conn.OpenAsync();
                    await using var cmd = new SqlCommand(sql.ToString(), conn);
                    cmd.Parameters.AddRange(parameters.ToArray());

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        books[reader.GetString(0)] = reader.GetInt32(1);
                    }
                    return books;
                });

            if (pr.FinalException != null)
                throw pr.FinalException;

            // Update entity IDs from books for newly inserted entities
            foreach (var entity in itemList.Where(e => e.GetId() == 0))
            {
                if (pr.Result.TryGetValue(entity.WebId!, out var newId))
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
                await PublishMessagesAsync(itemList, deleteList, session.Operation ?? "", username, schema);

            return SubmitOperation.CreateSuccess(inserted, updated, deleted, pr.Result);
        }
        catch (Exception ex)
        {
            return SubmitOperation.CreateFailure($"Submit failed: {schema} {ex.Message}", ex);
        }
    }

    private object? GetParameterValue(Column col, Entity entity, string json, string username, DateTimeOffset now, bool isInsert)
    {
        return col.Name switch
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
    }

    private static object? GetEntityPropertyValue(Column column, Entity entity)
    {
        var type = entity.GetType();
        var prop = type.GetProperty(column.Name!);
        if (prop is null)
            return DBNull.Value;

        var value = prop.GetValue(entity);
        if (value is null)
            return column.IsNullable ? DBNull.Value : null;

        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        return (propType.Name, value) switch
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
            _ when propType.IsEnum => value!.ToString(),
            _ => value
        };
    }

    private async Task PublishMessagesAsync(List<Entity> itemList, List<Entity> deleteList,
        string operation, string username, string schema)
    {
        if (messageBroker == null) return;

        try
        {
            // Publish messages for added/updated entities
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
                    AccountNo = schema,
                    Id = Guid.NewGuid().ToString("N")
                };

                await messageBroker.SendAsync(message);
            }

            // Publish messages for deleted entities
            foreach (var entity in deleteList)
            {
                var message = new BrokeredMessage(entity)
                {
                    Entity = entity.GetType().Name,
                    EntityId = entity.GetId(),
                    Crud = CrudOperation.Deleted,
                    Operation = operation,
                    Username = username,
                    AccountNo = schema,
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
