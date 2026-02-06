using System.Text;
using System.Text.RegularExpressions;

namespace MotoRent.SqlServerRepository;

public partial class Sql2012PagingTranslator : IPagingTranslator
{
    // Regex to check if SQL already has ORDER BY (case-insensitive, handles whitespace)
    [GeneratedRegex(@"\bORDER\s+BY\b", RegexOptions.IgnoreCase)]
    private static partial Regex OrderByRegex();

    // Regex to extract table name from FROM clause (e.g., "TillSession" from "FROM [MotoRent].[TillSession]")
    [GeneratedRegex(@"FROM\s+\[\w+\]\.\[(\w+)\]", RegexOptions.IgnoreCase)]
    private static partial Regex TableNameRegex();

    public string Translate(string sql, int page, int size)
    {
        var skipToken = (page - 1) * size;
        var output = new StringBuilder(sql);

        // SQL Server OFFSET/FETCH requires ORDER BY - add default if missing
        if (!OrderByRegex().IsMatch(sql))
        {
            // Extract table name to derive entity ID column (e.g., TillSession -> TillSessionId)
            var tableMatch = TableNameRegex().Match(sql);
            var orderColumn = tableMatch.Success ? $"{tableMatch.Groups[1].Value}Id" : "1";
            output.AppendLine();
            output.AppendFormat("ORDER BY [{0}]", orderColumn);
        }

        output.AppendLine();
        output.AppendFormat("OFFSET {0} ROWS", skipToken);
        output.AppendLine();
        output.AppendFormat("FETCH NEXT {0} ROWS ONLY", size);

        return output.ToString();
    }

    /// <summary>
    /// Translates SQL with computed column support.
    /// Detects computed columns in WHERE clause and ensures they're included in the subquery.
    /// </summary>
    public string TranslateWithComputedColumns(string sql, int page, int size, params string[] computedColumns)
    {
        var skipToken = (page - 1) * size;
        var output = new StringBuilder(sql);

        // SQL Server OFFSET/FETCH requires ORDER BY - add default if missing
        if (!OrderByRegex().IsMatch(sql))
        {
            // Extract table name to derive entity ID column (e.g., TillSession -> TillSessionId)
            var tableMatch = TableNameRegex().Match(sql);
            var orderColumn = tableMatch.Success ? $"{tableMatch.Groups[1].Value}Id" : "1";
            output.AppendLine();
            output.AppendFormat("ORDER BY [{0}]", orderColumn);
        }

        output.AppendLine();
        output.AppendFormat("OFFSET {0} ROWS", skipToken);
        output.AppendLine();
        output.AppendFormat("FETCH NEXT {0} ROWS ONLY", size);

        var resultSql = output.ToString();

        // If there are computed columns, we need to create a subquery that includes them
        if (computedColumns.Length > 0 && HasComputedColumnsInWhere(sql, computedColumns))
        {
            resultSql = CreateSubqueryWithComputedColumns(sql, computedColumns, skipToken, size);
        }

        return resultSql;
    }

    /// <summary>
    /// Checks if any computed columns are referenced in the WHERE clause.
    /// </summary>
    private static bool HasComputedColumnsInWhere(string sql, string[] computedColumns)
    {
        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        if (whereIndex < 0) return false;

        var whereClause = sql.Substring(whereIndex);
        return computedColumns.Any(col => whereClause.Contains($"[{col}]"));
    }

    // Regex to extract ORDER BY column from SQL (e.g., "[Name]" from "ORDER BY [Name]")
    [GeneratedRegex(@"ORDER\s+BY\s+\[(\w+)\]", RegexOptions.IgnoreCase)]
    private static partial Regex OrderByColumnRegex();

    /// <summary>
    /// Creates a subquery that includes computed columns in both inner and outer queries.
    /// </summary>
    private static string CreateSubqueryWithComputedColumns(string sql, string[] computedColumns, int skip, int size)
    {
        var selectIndex = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        var fromIndex = sql.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);

        if (selectIndex < 0 || fromIndex < 0) return sql;

        // Extract the original SELECT clause (without SELECT keyword)
        var originalSelect = sql.Substring(selectIndex + 6, fromIndex - selectIndex - 6).Trim();
        if (string.IsNullOrWhiteSpace(originalSelect)) return sql;

        // Extract original ORDER BY column (if any) to preserve sort order
        var orderByMatch = OrderByColumnRegex().Match(sql);
        string orderColumn;
        if (orderByMatch.Success)
        {
            orderColumn = orderByMatch.Groups[1].Value;
        }
        else
        {
            // Fallback: derive from table name (e.g., Shop -> ShopId)
            var tableMatch = TableNameRegex().Match(sql);
            orderColumn = tableMatch.Success ? $"{tableMatch.Groups[1].Value}Id" : "1";
        }

        // Build new SELECT with computed columns for WHERE clause filtering
        var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
        var additionalColumns = new List<string>();
        if (whereIndex >= 0)
        {
            var wherePart = sql.Substring(whereIndex);
            additionalColumns = computedColumns.Where(col => wherePart.Contains($"[{col}]")).ToList();
        }

        // Also include ORDER BY column if it's a computed column not already in original select
        if (computedColumns.Contains(orderColumn) && !originalSelect.Contains($"[{orderColumn}]"))
        {
            if (!additionalColumns.Contains(orderColumn))
            {
                additionalColumns.Add(orderColumn);
            }
        }

        var newSelect = additionalColumns.Count > 0
            ? $"{originalSelect}, {string.Join(", ", additionalColumns.Select(c => $"[{c}]"))}"
            : originalSelect;

        // Build inner query (without ORDER BY - ORDER BY should be on outer query with OFFSET/FETCH)
        var innerSql = sql.Substring(fromIndex);
        var orderByIndex = innerSql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        if (orderByIndex >= 0)
        {
            innerSql = innerSql.Substring(0, orderByIndex).TrimEnd();
        }
        var innerQuery = $"SELECT {newSelect} {innerSql}";

        // Build outer SELECT - include ORDER BY column if it's a computed column
        var outerSelect = originalSelect;
        if (computedColumns.Contains(orderColumn) && !originalSelect.Contains($"[{orderColumn}]"))
        {
            outerSelect = $"{originalSelect}, [{orderColumn}]";
        }

        // Build final query with subquery - ORDER BY must be before OFFSET/FETCH
        return $"SELECT {outerSelect} FROM ({innerQuery}) AS t1 ORDER BY [{orderColumn}] OFFSET {skip} ROWS FETCH NEXT {size} ROWS ONLY";
    }

    public string TranslateWithSkip(string sql, int top, int skip)
    {
        var output = new StringBuilder(sql);

        // SQL Server OFFSET/FETCH requires ORDER BY - add default if missing
        if (!OrderByRegex().IsMatch(sql))
        {
            // Extract table name to derive entity ID column (e.g., TillSession -> TillSessionId)
            var tableMatch = TableNameRegex().Match(sql);
            var orderColumn = tableMatch.Success ? $"{tableMatch.Groups[1].Value}Id" : "1";
            output.AppendLine();
            output.AppendFormat("ORDER BY [{0}]", orderColumn);
        }

        output.AppendLine();
        output.AppendLine($"OFFSET {skip} ROWS");
        output.AppendLine($"FETCH NEXT {top} ROWS ONLY");

        return output.ToString();
    }
}
