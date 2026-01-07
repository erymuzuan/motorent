using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public class LoadOperation<T> where T : Entity
{
    public List<T> ItemCollection { get; set; } = [];
    public int TotalRows { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRows / PageSize) : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
