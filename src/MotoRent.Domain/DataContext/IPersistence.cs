using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Provides batch transactional persistence for entities.
/// All inserts/updates/deletes execute in a single database transaction.
/// </summary>
public interface IPersistence
{
    /// <summary>
    /// Submits all changes in a single transaction.
    /// Returns a SubmitOperation with WebId-to-Id mapping for newly inserted entities.
    /// </summary>
    Task<SubmitOperation> SubmitChanges(
        IEnumerable<Entity> addedOrUpdatedItems,
        IEnumerable<Entity> deletedItems,
        PersistenceSession session);

    /// <summary>
    /// Submits a single entity (convenience method).
    /// </summary>
    Task<SubmitOperation> SubmitChanges(Entity item);
}
