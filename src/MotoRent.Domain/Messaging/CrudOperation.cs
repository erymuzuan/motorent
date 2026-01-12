namespace MotoRent.Domain.Messaging;

/// <summary>
/// Represents the type of CRUD operation performed on an entity.
/// </summary>
public enum CrudOperation
{
    None = 0,
    Added = 1,
    Changed = 2,
    Deleted = 3
}
