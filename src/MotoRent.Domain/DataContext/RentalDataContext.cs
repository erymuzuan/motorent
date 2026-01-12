using System.Linq.Expressions;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;

namespace MotoRent.Domain.DataContext;

public partial class RentalDataContext
{
    private QueryProvider QueryProvider { get; }
    private IMessageBroker? MessageBroker { get; }
    private string? AccountNo { get; }

    public RentalDataContext() : this(ObjectBuilder.GetObject<QueryProvider>()) { }

    public RentalDataContext(QueryProvider provider, IMessageBroker? messageBroker = null, string? accountNo = null)
    {
        this.QueryProvider = provider;
        this.MessageBroker = messageBroker;
        this.AccountNo = accountNo;
    }

    /// <summary>
    /// Creates a new query for the specified entity type.
    /// Preferred pattern over using Query properties directly.
    /// </summary>
    public Query<T> CreateQuery<T>() where T : Entity, new()
    {
        return new Query<T>(this.QueryProvider);
    }

    public async Task<T?> LoadOneAsync<T>(Expression<Func<T, bool>> predicate) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.LoadOneAsync(predicate);
    }

    public async Task<LoadOperation<T>> LoadAsync<T>(IQueryable<T> query,
        int page = 1, int size = 40, bool includeTotalRows = false) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.LoadAsync(query, page, size, includeTotalRows);
    }

    #region Aggregate Methods

    /// <summary>
    /// Gets the count of entities matching the query.
    /// </summary>
    public async Task<int> GetCountAsync<T>(IQueryable<T> query) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetCountAsync(query);
    }

    /// <summary>
    /// Checks if any entity matches the query.
    /// </summary>
    public async Task<bool> ExistAsync<T>(IQueryable<T> query) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.ExistAsync(query);
    }

    /// <summary>
    /// Gets the sum of a column for entities matching the query.
    /// </summary>
    public async Task<TResult> GetSumAsync<T, TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where T : Entity
        where TResult : struct
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetSumAsync(query, selector);
    }

    /// <summary>
    /// Gets the maximum value of a column for entities matching the query.
    /// </summary>
    public async Task<TResult> GetMaxAsync<T, TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetMaxAsync(query, selector);
    }

    /// <summary>
    /// Gets the minimum value of a column for entities matching the query.
    /// </summary>
    public async Task<TResult> GetMinAsync<T, TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetMinAsync(query, selector);
    }

    /// <summary>
    /// Gets the average value of a decimal column for entities matching the query.
    /// </summary>
    public async Task<decimal> GetAverageAsync<T>(IQueryable<T> query, Expression<Func<T, decimal>> selector)
        where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetAverageAsync(query, selector);
    }

    /// <summary>
    /// Gets a single scalar value from the first matching entity.
    /// </summary>
    public async Task<TResult?> GetScalarAsync<T, TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetScalarAsync(query, selector);
    }

    /// <summary>
    /// Gets distinct values for a column from entities matching the query.
    /// </summary>
    public async Task<List<TResult>> GetDistinctAsync<T, TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetDistinctAsync(query, selector);
    }

    #endregion

    public PersistenceSession OpenSession(string username = "system")
    {
        return new PersistenceSession(this, username);
    }

    internal async Task<SubmitOperation> SubmitChangesAsync(PersistenceSession session, string operation, string username)
    {
        int inserted = 0, updated = 0, deleted = 0;
        var processedEntities = new List<(Entity Entity, CrudOperation Crud)>();

        try
        {
            // Process deletes first
            foreach (var entity in session.DeletedCollection)
            {
                await DeleteEntityAsync(entity);
                deleted++;
                processedEntities.Add((entity, CrudOperation.Deleted));
            }

            // Process attached entities (insert or update)
            foreach (var entity in session.AttachedCollection)
            {
                if (entity.GetId() == 0)
                {
                    await InsertEntityAsync(entity, username);
                    inserted++;
                    processedEntities.Add((entity, CrudOperation.Added));
                }
                else
                {
                    await UpdateEntityAsync(entity, username);
                    updated++;
                    processedEntities.Add((entity, CrudOperation.Changed));
                }
            }

            // Publish messages to RabbitMQ if configured
            if (MessageBroker != null)
            {
                foreach (var (entity, crud) in processedEntities)
                {
                    await PublishMessageAsync(entity, crud, operation, username);
                }
            }

            return SubmitOperation.CreateSuccess(inserted, updated, deleted);
        }
        catch (Exception ex)
        {
            return SubmitOperation.CreateFailure($"Submit failed: {ex.Message}", ex);
        }
    }

    private async Task PublishMessageAsync(Entity entity, CrudOperation crud, string operation, string username)
    {
        if (MessageBroker == null) return;

        try
        {
            var message = new BrokeredMessage(entity)
            {
                Entity = entity.GetType().Name,
                EntityId = entity.GetId(),
                Crud = crud,
                Operation = operation,
                Username = username,
                AccountNo = AccountNo,
                Id = Guid.NewGuid().ToString("N")
            };

            await MessageBroker.SendAsync(message);
        }
        catch (Exception)
        {
            // Log but don't fail the operation if message publishing fails
            // The entity is already saved, messaging is async notification
        }
    }

    private async Task InsertEntityAsync(Entity entity, string username)
    {
        var method = typeof(RentalDataContext).GetMethod(nameof(InsertTypedAsync),
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
        var method = typeof(RentalDataContext).GetMethod(nameof(UpdateTypedAsync),
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
        var method = typeof(RentalDataContext).GetMethod(nameof(DeleteTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity])!;
    }

    private async Task DeleteTypedAsync<T>(T entity) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        await repos.DeleteAsync(entity);
    }
}
