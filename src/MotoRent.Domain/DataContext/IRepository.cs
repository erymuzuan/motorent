using System.Linq.Expressions;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public interface IRepository<T> where T : Entity
{
    Task<T?> LoadOneAsync(IQueryable<T> query);
    Task<T?> LoadOneAsync(Expression<Func<T, bool>> predicate);
    Task<LoadOperation<T>> LoadAsync(IQueryable<T> query, int page = 1, int size = 40, bool includeTotalRows = false);
    Task<int> InsertAsync(T entity, string username);
    Task<int> UpdateAsync(T entity, string username);
    Task<int> DeleteAsync(T entity);

    // Aggregate methods
    Task<int> GetCountAsync(IQueryable<T> query);
    Task<bool> ExistAsync(IQueryable<T> query);
    Task<TResult> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector) where TResult : struct;
    Task<TResult?> GetSumAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult?>> selector) where TResult : struct;
    Task<TResult> GetMaxAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);
    Task<TResult> GetMinAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);
    Task<decimal> GetAverageAsync(IQueryable<T> query, Expression<Func<T, decimal>> selector);
    Task<TResult?> GetScalarAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);
    Task<List<T>> GetListAsync(IQueryable<T> query, Expression<Func<T, object>> selector);
    Task<List<TResult>> GetDistinctAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);

    // Group aggregate methods
    Task<List<(TKey Key, int Count)>> GetGroupCountAsync<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector);
    Task<List<(TKey Key, TValue Sum)>> GetGroupSumAsync<TKey, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TValue>> valueSelector);
    Task<List<(TKey Key, TKey2 Key2, TValue Sum)>> GetGroupSumAsync<TKey, TKey2, TValue>(IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector, Expression<Func<T, TKey2>> key2Selector,
        Expression<Func<T, TValue>> valueSelector);

    // Multi-column list methods
    Task<List<(TResult, TResult2)>> GetList2Async<TResult, TResult2>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2);
    Task<List<(TResult, TResult2, TResult3)>> GetList3Async<TResult, TResult2, TResult3>(IQueryable<T> query,
        Expression<Func<T, TResult>> selector, Expression<Func<T, TResult2>> selector2,
        Expression<Func<T, TResult3>> selector3);
}
