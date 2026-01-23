using System.Text.RegularExpressions;

namespace MotoRent.Domain.Extensions;

public static class StringExtensions
{
    extension(string? value)
    {
        public string ToEmpty() => value ?? string.Empty;
        public string ToEmptyString() => value ?? string.Empty;
    }

    public static string ToString<T>(this IEnumerable<T> items, string separator, Func<T, string> selector)
    {
        return string.Join(separator, items.Select(selector));
    }

    public static string FlattenSql(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value, @"\s+", " ").Trim();
    }
}
