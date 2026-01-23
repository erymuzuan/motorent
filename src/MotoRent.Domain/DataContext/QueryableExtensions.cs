using MotoRent.Domain.Entities;

namespace MotoRent.Domain.DataContext;

public static class QueryableExtensions
{
public static string ToSqlCommand<T>(this IQueryable<T> query, string selectColumn) where T : Entity, new()
    {
        ReadOnlySpan<char> sql = query.ToString();
        var fi = sql.IndexOf("FROM");
        if (fi >= 0)
            return $"SELECT {selectColumn} {sql[fi..]}";
        throw new InvalidOperationException($"Cannot replace select column from query : {sql}");
    }

    /// <summary>
    /// Converts a query to a COUNT(*) SQL command, stripping any ORDER BY clause.
    /// ORDER BY is not valid with aggregate functions without GROUP BY in SQL Server.
    /// </summary>
    public static string ToCountSqlCommand<T>(this IQueryable<T> query) where T : Entity, new()
    {
        var sql = query.ToString() ?? string.Empty;
        var fi = sql.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
        if (fi < 0)
            throw new InvalidOperationException($"Cannot generate count SQL from query : {sql}");

        var fromClause = sql[fi..];

        // Strip ORDER BY clause - it's not valid with COUNT(*) without GROUP BY
        var orderByIndex = fromClause.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
        {
            fromClause = fromClause[..orderByIndex].TrimEnd();
        }

        return $"SELECT COUNT(*) {fromClause}";
    }

    /// <summary>
    /// Converts a query to SQL, ensuring computed columns used in WHERE clause are included in SELECT.
    /// This prevents "Invalid column name" errors when paging translator creates subqueries.
    /// </summary>
    public static string ToSqlCommandWithComputedColumns<T>(this IQueryable<T> query, string selectColumn, params string[] computedColumns) where T : Entity, new()
    {
        ReadOnlySpan<char> sql = query.ToString();
        var fi = sql.IndexOf("FROM");
        if (fi < 0)
            throw new InvalidOperationException($"Cannot replace select column from query : {sql}");

        var whereClause = sql.ToString();
        var whereIndex = whereClause.IndexOf("WHERE");
        var additionalColumns = new List<string>();

        // Check if any computed columns are referenced in the WHERE clause
        if (whereIndex >= 0 && computedColumns.Length > 0)
        {
            var wherePart = whereClause.Substring(whereIndex);
            foreach (var column in computedColumns)
            {
                if (wherePart.Contains($"[{column}]"))
                {
                    additionalColumns.Add($"[{column}]");
                }
            }
        }

        // Build SELECT clause with additional columns if needed
        var finalSelect = additionalColumns.Count > 0 
            ? $"{selectColumn}, {string.Join(", ", additionalColumns)}"
            : selectColumn;

        return $"SELECT {finalSelect} {sql[fi..]}";
    }

    public static string ToDeleteSqlCommand<T>(this IQueryable<T> query) where T : Entity, new()
    {
        ReadOnlySpan<char> sql = query.ToString();
        var fi = sql.IndexOf("FROM");
        if (fi >= 0)
            return $"DELETE {sql[fi..]}";
        throw new InvalidOperationException($"Cannot replace select column from query : {sql}");
    }
}
