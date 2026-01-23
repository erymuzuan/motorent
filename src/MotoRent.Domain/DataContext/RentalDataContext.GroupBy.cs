using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Group by aggregate methods for RentalDataContext.
/// Use these for COUNT(*) GROUP BY and SUM() GROUP BY queries.
/// </summary>
public partial class RentalDataContext
{
    #region GetGroupByCountAsync

    /// <summary>
    /// Gets count grouped by a key column.
    /// Example: Get rental count by status.
    /// </summary>
    public async Task<List<(TKey Key, int Count)>> GetGroupByCountAsync<T, TKey>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetGroupCountAsync(query, keySelector);
    }

    /// <summary>
    /// Gets count grouped by a key column from entities matching the predicate.
    /// Example: Get rental count by status for a specific shop.
    /// </summary>
    public async Task<List<(TKey Key, int Count)>> GetGroupByCountAsync<T, TKey>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> keySelector) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetGroupCountAsync(query, keySelector);
    }

    #endregion

    #region GetGroupBySumAsync (Single Key)

    /// <summary>
    /// Gets sum grouped by a key column.
    /// Example: Get total revenue by payment method.
    /// </summary>
    public async Task<List<(TKey Key, TValue Sum)>> GetGroupBySumAsync<T, TKey, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TValue>> valueSelector) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetGroupSumAsync(query, keySelector, valueSelector);
    }

    /// <summary>
    /// Gets sum grouped by a key column from entities matching the predicate.
    /// Example: Get total revenue by payment method for a specific shop.
    /// </summary>
    public async Task<List<(TKey Key, TValue Sum)>> GetGroupBySumAsync<T, TKey, TValue>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TValue>> valueSelector) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetGroupSumAsync(query, keySelector, valueSelector);
    }

    #endregion

    #region GetGroupBySumAsync (Two Keys)

    /// <summary>
    /// Gets sum grouped by two key columns.
    /// Example: Get total revenue by shop and payment method.
    /// </summary>
    public async Task<List<(TKey Key, TKey2 Key2, TValue Sum)>> GetGroupBySumAsync<T, TKey, TKey2, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TKey2>> key2Selector,
        Expression<Func<T, TValue>> valueSelector) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetGroupSumAsync(query, keySelector, key2Selector, valueSelector);
    }

    /// <summary>
    /// Gets sum grouped by two key columns from entities matching the predicate.
    /// Example: Get total revenue by shop and payment method for a date range.
    /// </summary>
    public async Task<List<(TKey Key, TKey2 Key2, TValue Sum)>> GetGroupBySumAsync<T, TKey, TKey2, TValue>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TKey2>> key2Selector,
        Expression<Func<T, TValue>> valueSelector) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetGroupSumAsync(query, keySelector, key2Selector, valueSelector);
    }

    #endregion
}
