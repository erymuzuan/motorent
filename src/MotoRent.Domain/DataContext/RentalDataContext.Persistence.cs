using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Persistence methods for RentalDataContext.
/// Handles SubmitChanges, CRUD operations, and message publishing.
/// </summary>
public partial class RentalDataContext
{
    internal async Task<SubmitOperation> SubmitChangesAsync(PersistenceSession session)
    {
        // Use injected IPersistence if available
        if (this.Persistence != null)
        {
            return await this.Persistence.SubmitChanges(
                session.AttachedCollection,
                session.DeletedCollection,
                session);
        }

        // Fallback to ObjectBuilder for backward compatibility
        var persistence = ObjectBuilder.GetObjectOrDefault<IPersistence>();
        if (persistence != null)
        {
            return await persistence.SubmitChanges(
                session.AttachedCollection,
                session.DeletedCollection,
                session);
        }

        // Legacy fallback: reflection-based individual calls
        return await this.LegacySubmitChangesAsync(session);
    }

    private async Task<SubmitOperation> LegacySubmitChangesAsync(PersistenceSession session)
    {
        int inserted = 0, updated = 0, deleted = 0;
        var processedEntities = new List<(Entity Entity, CrudOperation Crud)>();
        var username = session.Username;
        var operation = session.Operation ?? "";

        try
        {
            // Process deletes first
            foreach (var entity in session.DeletedCollection)
            {
                await this.DeleteEntityAsync(entity);
                deleted++;
                processedEntities.Add((entity, CrudOperation.Deleted));
            }

            // Process attached entities (insert or update)
            foreach (var entity in session.AttachedCollection)
            {
                if (entity.GetId() == 0)
                {
                    await this.InsertEntityAsync(entity, username);
                    inserted++;
                    processedEntities.Add((entity, CrudOperation.Added));
                }
                else
                {
                    await this.UpdateEntityAsync(entity, username);
                    updated++;
                    processedEntities.Add((entity, CrudOperation.Changed));
                }
            }

            // Publish messages to RabbitMQ if configured
            if (this.MessageBroker != null)
            {
                foreach (var (entity, crud) in processedEntities)
                {
                    await this.PublishMessageAsync(entity, crud, operation, username);
                }
            }

            return SubmitOperation.CreateSuccess(inserted, updated, deleted);
        }
        catch (Exception ex)
        {
            return SubmitOperation.CreateFailure($"Submit failed: {this.AccountNo} {ex.Message}", ex);
        }
    }

    private async Task PublishMessageAsync(Entity entity, CrudOperation crud, string operation, string username)
    {
        if (this.MessageBroker == null) return;

        try
        {
            var message = new BrokeredMessage(entity)
            {
                Entity = entity.GetType().Name,
                EntityId = entity.GetId(),
                Crud = crud,
                Operation = operation,
                Username = username,
                AccountNo = this.AccountNo,
                Id = Guid.NewGuid().ToString("N")
            };

            await this.MessageBroker.SendAsync(message);
        }
        catch (Exception)
        {
            // Log but don't fail the operation if message publishing fails
            // The entity is already saved, messaging is async notification
        }
    }

    #region Reflection-based CRUD (Legacy Fallback)

    private async Task InsertEntityAsync(Entity entity, string username)
    {
        var method = typeof(RentalDataContext).GetMethod(nameof(this.InsertTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity, username])!;
    }

    private async Task InsertTypedAsync<T>(T entity, string username) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        await repos.InsertAsync(entity, username);
    }

    private async Task UpdateEntityAsync(Entity entity, string username)
    {
        var method = typeof(RentalDataContext).GetMethod(nameof(this.UpdateTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity, username])!;
    }

    private async Task UpdateTypedAsync<T>(T entity, string username) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        await repos.UpdateAsync(entity, username);
    }

    private async Task DeleteEntityAsync(Entity entity)
    {
        var method = typeof(RentalDataContext).GetMethod(nameof(this.DeleteTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity])!;
    }

    private async Task DeleteTypedAsync<T>(T entity) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        await repos.DeleteAsync(entity);
    }

    #endregion
}
