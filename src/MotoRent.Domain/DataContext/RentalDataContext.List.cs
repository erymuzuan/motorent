using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

/// <summary>
/// List methods for RentalDataContext.
/// Includes tuple-based lists (2-3 columns) and DataMap-based lists (N columns).
/// </summary>
public partial class RentalDataContext
{
    #region GetListAsync (2 Columns - Tuple)

    /// <summary>
    /// Gets a list of two columns from entities matching the query.
    /// </summary>
    public async Task<List<(TResult, TResult2)>> GetListAsync<T, TResult, TResult2>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetList2Async(query, selector, selector2);
    }

    /// <summary>
    /// Gets a list of two columns from entities matching the predicate.
    /// </summary>
    public async Task<List<(TResult, TResult2)>> GetListAsync<T, TResult, TResult2>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetList2Async(query, selector, selector2);
    }

    #endregion

    #region GetListAsync (3 Columns - Tuple)

    /// <summary>
    /// Gets a list of three columns from entities matching the query.
    /// </summary>
    public async Task<List<(TResult, TResult2, TResult3)>> GetListAsync<T, TResult, TResult2, TResult3>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2,
        Expression<Func<T, TResult3>> selector3) where T : Entity
    {
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetList3Async(query, selector, selector2, selector3);
    }

    /// <summary>
    /// Gets a list of three columns from entities matching the predicate.
    /// </summary>
    public async Task<List<(TResult, TResult2, TResult3)>> GetListAsync<T, TResult, TResult2, TResult3>(Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2,
        Expression<Func<T, TResult3>> selector3) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetList3Async(query, selector, selector2, selector3);
    }

    #endregion

    #region GetListAsync (N Columns - DataMap)

    /// <summary>
    /// Gets a list of DataMap objects containing only the specified columns.
    /// Performance-optimized: only requested columns are fetched, no JSON deserialization.
    /// Use for read-only displays (lists, tables, dropdowns) instead of LoadAsync.
    /// </summary>
    /// <typeparam name="T">The entity type to query</typeparam>
    /// <param name="query">The query to execute</param>
    /// <param name="fieldSelectors">Expressions selecting the columns to fetch</param>
    /// <returns>Array of DataMap objects with the requested column values</returns>
    public async Task<DataMap<T>[]> GetListAsync<T>(IQueryable<T> query,
        params Expression<Func<T, object>>[] fieldSelectors) where T : Entity
    {
        if (fieldSelectors.Length == 0)
            throw new ArgumentException("At least one field selector must be specified", nameof(fieldSelectors));

        var columns = fieldSelectors
            .Select(GetMemberName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();

        if (columns.Length == 0)
            throw new ArgumentException("No valid column names could be extracted from selectors", nameof(fieldSelectors));

        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        var dataReader = await repos.GetReaderAsync(query, columns!);
        await using var reader = (DbDataReader)dataReader;

        var results = new List<DataMap<T>>();
        while (await reader.ReadAsync())
        {
            var map = new DataMap<T>();
            for (var i = 0; i < columns.Length; i++)
            {
                var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                map[columns[i]!] = value;
            }
            results.Add(map);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Gets a list of DataMap objects containing only the specified columns.
    /// Performance-optimized: only requested columns are fetched, no JSON deserialization.
    /// </summary>
    /// <typeparam name="T">The entity type to query</typeparam>
    /// <param name="predicate">Predicate to filter entities</param>
    /// <param name="fieldSelectors">Expressions selecting the columns to fetch</param>
    /// <returns>Array of DataMap objects with the requested column values</returns>
    public async Task<DataMap<T>[]> GetListAsync<T>(Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] fieldSelectors) where T : Entity, new()
    {
        var query = CreateQuery<T>().Where(predicate);
        return await GetListAsync(query, fieldSelectors);
    }

    #endregion

    #region GetReaderAsync

    /// <summary>
    /// Gets a data reader for specific columns without loading full entities.
    /// Caller is responsible for disposing the reader.
    /// </summary>
    public async Task<IDataReader> GetReaderAsync<T>(IQueryable<T> query,
        params Expression<Func<T, object>>[] fieldSelectors) where T : Entity
    {
        var columns = fieldSelectors
            .Select(GetMemberName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();

        if (columns.Length == 0)
            throw new ArgumentException("No valid column names could be extracted from selectors", nameof(fieldSelectors));

        var repos = ObjectBuilder.GetObject<IRepository<T>>();
        return await repos.GetReaderAsync(query, columns!);
    }

    #endregion

    #region Helper Methods

    private static string? GetMemberName<T>(Expression<Func<T, object>> selector)
    {
        // Handle direct member access: t => t.Property
        if (selector.Body is MemberExpression memberExpr)
            return memberExpr.Member.Name;

        // Handle Convert for value types: t => (object)t.ValueTypeProperty
        if (selector.Body is UnaryExpression { Operand: MemberExpression unaryMember })
            return unaryMember.Member.Name;

        return null;
    }

    #endregion
}
