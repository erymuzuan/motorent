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
}
