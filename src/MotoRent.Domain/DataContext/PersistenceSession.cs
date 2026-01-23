using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public sealed class PersistenceSession : IDisposable
{
    private RentalDataContext? m_context;
    private readonly string m_username;

    internal List<Entity> AttachedCollection { get; } = [];
    internal List<Entity> DeletedCollection { get; } = [];

    /// <summary>
    /// Gets the username for audit fields.
    /// </summary>
    public string Username => m_username;

    /// <summary>
    /// Gets the operation name for messaging/logging.
    /// </summary>
    public string? Operation { get; private set; }

    public PersistenceSession(RentalDataContext context, string username = "system")
    {
        m_context = context;
        m_username = username;
    }

    public void Attach<T>(params T[] items) where T : Entity
    {
        if (m_context == null)
            throw new ObjectDisposedException("Session has been completed");

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.WebId))
                item.WebId = Guid.NewGuid().ToString();
            AttachedCollection.Add(item);
        }
    }

    public void Delete(params Entity[] entities)
    {
        if (m_context == null)
            throw new ObjectDisposedException("Session has been completed");

        DeletedCollection.AddRange(entities);
    }

    public async Task<SubmitOperation> SubmitChanges(string operation = "")
    {
        if (m_context == null)
            throw new ObjectDisposedException("Session has been completed");

        this.Operation = operation;
        try
        {
            var so = await m_context.SubmitChangesAsync(this);
            AttachedCollection.Clear();
            DeletedCollection.Clear();
            m_context = null;
            return so;
        }
        catch (Exception ex)
        {
            return SubmitOperation.CreateFailure(ex.Message, ex);
        }
    }

    public void Dispose()
    {
        m_context = null;
    }
}
