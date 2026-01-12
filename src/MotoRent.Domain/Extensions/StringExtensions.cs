namespace MotoRent.Domain.Extensions;

/// <summary>
/// Extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns an empty string if the input is null.
    /// </summary>
    public static string ToEmpty(this string? value) => value ?? string.Empty;

    /// <summary>
    /// Alias for ToEmpty - returns empty string if null.
    /// </summary>
    public static string ToEmptyString(this string? value) => value ?? string.Empty;

    /// <summary>
    /// Joins a collection of items with a separator, applying a selector function.
    /// </summary>
    public static string ToString<T>(this IEnumerable<T> items, string separator, Func<T, string> selector)
    {
        return string.Join(separator, items.Select(selector));
    }
}
