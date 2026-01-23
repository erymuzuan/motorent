using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Loading methods for RentalDataContext.
/// </summary>
public partial class RentalDataContext
{
    /// <summary>
    /// Loads a single entity matching the predicate.
    /// </summary>
    public async Task<T?> LoadOneAsync<T>(Expression<Func<T, bool>> predicate) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.LoadOneAsync(predicate);
    }

    /// <summary>
    /// Loads a single entity matching the query.
    /// Preferred for complex queries with chained Where calls.
    /// </summary>
    public async Task<T?> LoadOneAsync<T>(IQueryable<T> query) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.LoadOneAsync(query);
    }

    /// <summary>
    /// Loads entities matching the query with pagination.
    /// </summary>
    public async Task<LoadOperation<T>> LoadAsync<T>(IQueryable<T> query,
        int page = 1, int size = 40, bool includeTotalRows = false) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.LoadAsync(query, page, size, includeTotalRows);
    }
}
