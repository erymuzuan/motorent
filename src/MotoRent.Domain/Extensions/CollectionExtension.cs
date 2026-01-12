namespace MotoRent.Domain.Extensions;

/// <summary>
/// Extension methods for collections, providing SQL-friendly query operations.
/// </summary>
public static class CollectionExtension
{
    /// <summary>
    /// Checks if an item is in a list. This method is recognized by the query provider
    /// and translated to SQL IN clause. Use this instead of List.Contains() in LINQ Where clauses.
    /// </summary>
    /// <example>
    /// var ids = new[] { 1, 2, 3 };
    /// var query = context.CreateQuery&lt;Entity&gt;().Where(e => ids.IsInList(e.Id));
    /// // Translates to: WHERE [Id] IN (1, 2, 3)
    /// </example>
    public static bool IsInList<T>(this IEnumerable<T> list, T item)
    {
        return list.Contains(item);
    }
}
