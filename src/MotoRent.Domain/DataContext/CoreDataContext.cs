using System.Linq.Expressions;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Helps;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Data context for Core schema entities (shared across all tenants).
/// Manages Organization, User, Setting, AccessToken, RegistrationInvite, and LogEntry entities.
/// </summary>
public class CoreDataContext
{
    public Query<Organization> Organizations { get; }
    public Query<User> Users { get; }
    public Query<Setting> Settings { get; }
    public Query<AccessToken> AccessTokens { get; }
    public Query<RegistrationInvite> RegistrationInvites { get; }
    public Query<LogEntry> LogEntries { get; }
    public Query<SupportRequest> SupportRequests { get; }

    private QueryProvider QueryProvider { get; }

    public CoreDataContext() : this(ObjectBuilder.GetObject<QueryProvider>()) { }

    public CoreDataContext(QueryProvider provider)
    {
        QueryProvider = provider;
        Organizations = new Query<Organization>(provider);
        Users = new Query<User>(provider);
        Settings = new Query<Setting>(provider);
        AccessTokens = new Query<AccessToken>(provider);
        RegistrationInvites = new Query<RegistrationInvite>(provider);
        LogEntries = new Query<LogEntry>(provider);
        SupportRequests = new Query<SupportRequest>(provider);
    }

    /// <summary>
    /// Loads a single entity matching the predicate.
    /// </summary>
    public async Task<T?> LoadOneAsync<T>(Expression<Func<T, bool>> predicate) where T : Entity
    {
        var repos = GetRepository<T>();
        return await repos.LoadOneAsync(predicate);
    }

    /// <summary>
    /// Loads entities matching the query with pagination.
    /// </summary>
    public async Task<LoadOperation<T>> LoadAsync<T>(IQueryable<T> query,
        int page = 1, int size = 40, bool includeTotalRows = false) where T : Entity
    {
        var repos = GetRepository<T>();
        return await repos.LoadAsync(query, page, size, includeTotalRows);
    }

    /// <summary>
    /// Opens a persistence session for batch operations.
    /// </summary>
    public CorePersistenceSession OpenSession(string username = "system")
    {
        return new CorePersistenceSession(this, username);
    }

    /// <summary>
    /// Gets or creates a CoreRepository for the specified entity type.
    /// </summary>
    private CoreRepository<T> GetRepository<T>() where T : Entity
    {
        return new CoreRepository<T>(QueryProvider);
    }

    internal async Task<SubmitOperation> SubmitChangesAsync(CorePersistenceSession session, string operation, string username)
    {
        int inserted = 0, updated = 0, deleted = 0;

        try
        {
            // Process deletes first
            foreach (var entity in session.DeletedCollection)
            {
                await DeleteEntityAsync(entity);
                deleted++;
            }

            // Process attached entities (insert or update)
            foreach (var entity in session.AttachedCollection)
            {
                if (entity.GetId() == 0)
                {
                    await InsertEntityAsync(entity, username);
                    inserted++;
                }
                else
                {
                    await UpdateEntityAsync(entity, username);
                    updated++;
                }
            }

            return SubmitOperation.CreateSuccess(inserted, updated, deleted);
        }
        catch (Exception ex)
        {
            return SubmitOperation.CreateFailure($"Submit failed: {ex.Message}", ex);
        }
    }

    private async Task InsertEntityAsync(Entity entity, string username)
    {
        var method = typeof(CoreDataContext).GetMethod(nameof(InsertTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity, username])!;
    }

    private async Task InsertTypedAsync<T>(T entity, string username) where T : Entity
    {
        var repos = GetRepository<T>();
        await repos.InsertAsync(entity, username);
    }

    private async Task UpdateEntityAsync(Entity entity, string username)
    {
        var method = typeof(CoreDataContext).GetMethod(nameof(UpdateTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity, username])!;
    }

    private async Task UpdateTypedAsync<T>(T entity, string username) where T : Entity
    {
        var repos = GetRepository<T>();
        await repos.UpdateAsync(entity, username);
    }

    private async Task DeleteEntityAsync(Entity entity)
    {
        var method = typeof(CoreDataContext).GetMethod(nameof(DeleteTypedAsync),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(entity.GetType());
        await (Task)genericMethod.Invoke(this, [entity])!;
    }

    private async Task DeleteTypedAsync<T>(T entity) where T : Entity
    {
        var repos = GetRepository<T>();
        await repos.DeleteAsync(entity);
    }
}

/// <summary>
/// Persistence session for CoreDataContext operations.
/// </summary>
public class CorePersistenceSession : IDisposable
{
    private readonly CoreDataContext m_context;
    private readonly string m_username;

    internal List<Entity> AttachedCollection { get; } = [];
    internal List<Entity> DeletedCollection { get; } = [];

    public CorePersistenceSession(CoreDataContext context, string username)
    {
        m_context = context;
        m_username = username;
    }

    /// <summary>
    /// Attaches an entity for insert or update.
    /// </summary>
    public void Attach(Entity entity)
    {
        if (!AttachedCollection.Contains(entity))
            AttachedCollection.Add(entity);
    }

    /// <summary>
    /// Marks an entity for deletion.
    /// </summary>
    public void Delete(Entity entity)
    {
        if (!DeletedCollection.Contains(entity))
            DeletedCollection.Add(entity);
    }

    /// <summary>
    /// Submits all pending changes to the database.
    /// </summary>
    public async Task<SubmitOperation> SubmitChanges(string operation = "")
    {
        return await m_context.SubmitChangesAsync(this, operation, m_username);
    }

    public void Dispose()
    {
        AttachedCollection.Clear();
        DeletedCollection.Clear();
    }
}
