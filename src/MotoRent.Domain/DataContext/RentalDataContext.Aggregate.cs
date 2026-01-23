using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// Aggregate methods for RentalDataContext: Count, Sum, Max, Min, Average, Scalar, Distinct.
/// </summary>
public partial class RentalDataContext
{
    #region Count

    /// <summary>
    /// Gets the count of entities matching the query.
    /// </summary>
    public async Task<int> GetCountAsync<T>(IQueryable<T> query) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetCountAsync(query);
    }

    /// <summary>
    /// Gets count from entities matching the predicate.
    /// </summary>
    public async Task<int> GetCountAsync<T>(Expression<Func<T, bool>> predicate) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetCountAsync(query);
    }

    #endregion

    #region Exist / Any

    /// <summary>
    /// Checks if any entity matches the query.
    /// </summary>
    public async Task<bool> ExistAsync<T>(IQueryable<T> query) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.ExistAsync(query);
    }

    /// <summary>
    /// Checks if any entity matches the predicate.
    /// </summary>
    public async Task<bool> GetAnyAsync<T>(Expression<Func<T, bool>> predicate) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.ExistAsync(query);
    }

    #endregion

    #region Sum

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
    /// Gets sum of nullable values from entities matching the query.
    /// </summary>
    public async Task<TResult?> GetSumAsync<T, TResult>(IQueryable<T> query, Expression<Func<T, TResult?>> selector)
        where T : Entity
        where TResult : struct
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetSumAsync(query, selector);
    }

    /// <summary>
    /// Gets sum of values from entities matching the predicate.
    /// </summary>
    public async Task<TResult> GetSumAsync<T, TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        where T : Entity, new()
        where TResult : struct
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetSumAsync(query, selector);
    }

    #endregion

    #region Max / Min

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
    /// Gets the maximum value from entities matching the predicate.
    /// </summary>
    public async Task<TResult> GetMaxAsync<T, TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
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
    /// Gets the minimum value from entities matching the predicate.
    /// </summary>
    public async Task<TResult> GetMinAsync<T, TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetMinAsync(query, selector);
    }

    #endregion

    #region Average

    /// <summary>
    /// Gets the average value of a decimal column for entities matching the query.
    /// </summary>
    public async Task<decimal> GetAverageAsync<T>(IQueryable<T> query, Expression<Func<T, decimal>> selector)
        where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetAverageAsync(query, selector);
    }

    #endregion

    #region Scalar

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
    /// Gets a single scalar value from entities matching the predicate.
    /// </summary>
    public async Task<TResult?> GetScalarAsync<T, TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetScalarAsync(query, selector);
    }

    #endregion

    #region Distinct

    /// <summary>
    /// Gets distinct values for a column from entities matching the query.
    /// </summary>
    public async Task<List<TResult>> GetDistinctAsync<T, TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector)
        where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetDistinctAsync(query, selector);
    }

    /// <summary>
    /// Gets distinct values for a column from entities matching the predicate.
    /// </summary>
    public async Task<List<TResult>> GetDistinctAsync<T, TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
        where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetDistinctAsync(query, selector);
    }

    #endregion
}
