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
    Task<TResult> GetMaxAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);
    Task<TResult> GetMinAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);
    Task<decimal> GetAverageAsync(IQueryable<T> query, Expression<Func<T, decimal>> selector);
    Task<TResult?> GetScalarAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);
    Task<List<T>> GetListAsync(IQueryable<T> query, Expression<Func<T, object>> selector);
    Task<List<TResult>> GetDistinctAsync<TResult>(IQueryable<T> query, Expression<Func<T, TResult>> selector);
}
