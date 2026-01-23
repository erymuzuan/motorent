using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;
using MotoRent.Domain.QueryProviders;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Main data context for rental operations.
/// Split into partial classes by logical function:
/// - RentalDataContext.cs (this file): Core - constructor, CreateQuery, OpenSession
/// - RentalDataContext.Load.cs: Loading methods - LoadOneAsync, LoadAsync
/// - RentalDataContext.Aggregate.cs: Aggregate methods - Count, Sum, Max, Min, Average, Scalar, Distinct
/// - RentalDataContext.GroupBy.cs: Group by methods - GetGroupByCountAsync, GetGroupBySumAsync
/// - RentalDataContext.List.cs: List methods - GetListAsync (tuples, DataMap), GetReaderAsync
/// - RentalDataContext.Persistence.cs: Persistence - SubmitChanges, CRUD operations
/// </summary>
public partial class RentalDataContext
{
    private QueryProvider QueryProvider { get; }
    private IMessageBroker? MessageBroker { get; }
    private string? AccountNo { get; }
    private IPersistence? Persistence { get; }

    public RentalDataContext() : this(ObjectBuilder.GetObject<QueryProvider>()) { }

    public RentalDataContext(QueryProvider provider, IMessageBroker? messageBroker = null,
        string? accountNo = null, IPersistence? persistence = null)
    {
        this.QueryProvider = provider;
        this.MessageBroker = messageBroker;
        this.AccountNo = accountNo;
        this.Persistence = persistence;
    }

    /// <summary>
    /// Creates a new query for the specified entity type.
    /// Preferred pattern over using Query properties directly.
    /// </summary>
    public IQueryable<T> CreateQuery<T>() where T : Entity, new()
    {
        return new Query<T>(this.QueryProvider);
    }

    /// <summary>
    /// Opens a persistence session for batch operations.
    /// </summary>
    public PersistenceSession OpenSession(string username = "system")
    {
        return new PersistenceSession(this, username);
    }
}
