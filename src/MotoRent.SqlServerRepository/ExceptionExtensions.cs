using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;

namespace MotoRent.SqlServerRepository;

/// <summary>
/// Extension methods for handling SQL exceptions related to missing database objects.
/// Automatically creates missing tables and columns from SQL definition files in the database folder.
/// </summary>
public static class ExceptionExtensions
{
    extension(Exception ex)
    {
        /// <summary>
        /// Parses SQL exception to extract missing table name from "Invalid object name" error.
        /// </summary>
        public (bool MissingTable, string? Schema, string? Table) GetInvalidTableName()
        {
            if (ex is SqlException ce
                && ce.Message.Contains("Invalid object name")
                && ce.Message.Split([".", "'"], StringSplitOptions.RemoveEmptyEntries) is [.., var schema, var table])
                return (true, schema, table);
            return (false, null, null);
        }

        /// <summary>
        /// Parses SQL exception to extract missing column name from "Invalid column name" error.
        /// </summary>
        private (bool MissingColumn, string? Column) GetInvalidColumnName()
        {
            if (ex is SqlException ce
                && ce.Message.Contains("Invalid column name")
                && ce.Message.Split([".", "'"], StringSplitOptions.RemoveEmptyEntries) is [.., var schema, var column])
                return (true, column);
            return (false, null);
        }
    }

    /// <summary>
    /// Attempts to create missing SQL objects (tables, columns) based on the exception.
    /// Uses SQL definition files from the database/tables folder.
    /// </summary>
    /// <param name="ex">The exception to analyze</param>
    /// <param name="schema">The tenant schema name (AccountNo)</param>
    /// <param name="tables">Tables to check for missing columns</param>
    /// <returns>True if a SQL object was created, false otherwise</returns>
    public static async Task<bool> CreateSqlObjectIfMissingAsync(this Exception? ex, string schema,
        params string[] tables)
    {
        if (ex is null) return false;
        if (ex is AggregateException { InnerExceptions: [var ne] })
            return await ne.CreateSqlObjectIfMissingAsync(schema, tables);

        if (ex.GetInvalidTableName() is (true, { Length: > 0 } schema2, { Length: > 0 } table2))
        {
            var created = await CreateTableAsync(schema2, table2);
            return created;
        }

        foreach (var table in tables)
        {
            if (ex.GetInvalidColumnName() is (true, "ChangedTimestamp"))
            {
                var result = await CreateChangedTimestampColumnAsync(schema, table);
                if (result) return true;
            }
            if (ex.GetInvalidColumnName() is (true, "CreatedTimestamp"))
            {
                var result = await CreateCreatedTimestampColumnAsync(schema, table);
                if (result) return true;
            }
            if (ex.GetInvalidColumnName() is (true, { Length: > 0 } column))
            {
                var result = await CreateColumnAsync(schema, table, column);
                if (result) return true;
            }
        }

        return false;
    }

    private static async Task<bool> CreateTableAsync(string schema, string table)
    {
        var folder = Path.Combine(MotoConfig.DatabaseSource, "tables");
        if (!Directory.Exists(folder))
        {
            Console.WriteLine($"[CreateTable] Database source folder not found: {folder}");
            return false;
        }

        var files = Directory.GetFiles(folder, "*.sql")
            .Where(x => x.EndsWith($".{table}.sql")).ToArray();
        if (files is not [var file])
        {
            throw new FileNotFoundException($"[CreateTable] SQL file not found for table '{schema}.{table}'. Expected file: *{table}.sql in {folder}");
        }

        var sql = await File.ReadAllTextAsync(file);
        if (sql.Contains("[Core]"))
        {
            Console.WriteLine($"[CreateTable] Cannot auto-create Core table '{table}' from {file}");
            return false;
        }

        // Replace schema placeholder with actual tenant schema
        sql = sql.Replace("[MotoRent].", $"[{schema}].");
        sql = sql.Replace("<schema>", schema);

        if (string.IsNullOrWhiteSpace(MotoConfig.SqlConnectionString))
        {
            Console.WriteLine($"[CreateTable] No connection string found for schema '{schema}'");
            return false;
        }

        await using var conn = new SqlConnection(MotoConfig.SqlConnectionString);
        await using var cmd = new SqlCommand(sql, conn);
        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"[CreateTable] Successfully created table '{schema}.{table}' from {file}");
        return true;
    }

    private static async Task<bool> CreateColumnAsync(string schema, string table, string column)
    {
        var folder = Path.Combine(MotoConfig.DatabaseSource, "tables");
        if (!Directory.Exists(folder))
        {
            Console.WriteLine($"[CreateColumn] Database source folder not found: {folder}");
            return false;
        }

        var files = Directory.GetFiles(folder, "*.sql")
            .Where(x => x.EndsWith($"{table}.sql", StringComparison.InvariantCultureIgnoreCase)).ToArray();
        if (files is not [var file]) return false;


        var text = await File.ReadAllTextAsync(file);
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var ct = "";

        bool ColumnLine(string line)
        {
            if (line.Trim().Contains($"[{column}] DATE NULL", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (line.Trim().Contains($"[{column}] DATETIMEOFFSET NULL", StringComparison.InvariantCultureIgnoreCase)) return true;
            if (!line.Contains($"[{column}]", StringComparison.InvariantCultureIgnoreCase)) return false;
            if (!line.Contains(" AS ", StringComparison.InvariantCultureIgnoreCase)) return false;
            if (!line.Contains(" CAST(", StringComparison.InvariantCultureIgnoreCase)) return false;

            return true;
        }

        foreach (var line in lines)
        {
            if (!ColumnLine(line)) continue;
            if (line.Trim().EndsWith(','))
                ct = line.Trim()[..^1];
        }

        if (string.IsNullOrWhiteSpace(ct)) return false;

        var sql = $"""
                   ALTER TABLE [{schema}].[{table}]
                   ADD {ct};
                   """;

        if (string.IsNullOrWhiteSpace(MotoConfig.SqlConnectionString))
        {
            Console.WriteLine($"[CreateColumn] No connection string found for schema '{schema}'");
            return false;
        }

        try
        {
            await using var conn = new SqlConnection(MotoConfig.SqlConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"[CreateColumn] Successfully added column '{column}' to '{schema}.{table}'");
        }
        catch (SqlException e) when (e.Message.Contains("Column names in each table must be unique",
                                         StringComparison.InvariantCultureIgnoreCase))
        {
            Console.WriteLine($"[CreateColumn] Column '{column}' already exists in '{schema}.{table}': {e.Message}");
            return false;
        }

        return true;
    }

    private static async Task<bool> CreateChangedTimestampColumnAsync(string schema, string table)
    {
        var sql = $"""
                   ALTER TABLE [{schema}].[{table}]
                   ADD [ChangedTimestamp] DATETIMEOFFSET NULL;

                   UPDATE [{schema}].[{table}]
                   SET [ChangedTimestamp] = [ChangedDate]
                   WHERE [ChangedTimestamp] IS NULL;

                   ALTER TABLE [{schema}].[{table}]
                   DROP COLUMN [ChangedDate];
                   """;

        if (string.IsNullOrWhiteSpace(MotoConfig.SqlConnectionString))
        {
            Console.WriteLine($"[CreateChangedTimestampColumn] No connection string found for schema '{schema}'");
            return false;
        }

        foreach (var ts in sql.Split([";"], StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                await using var conn = new SqlConnection(MotoConfig.SqlConnectionString);
                await using var cmd = new SqlCommand(ts.Trim(), conn);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException e) when (e.Message.Contains("Column names in each table must be unique",
                                             StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"[CreateChangedTimestampColumn] {e.Message}");
                return false;
            }
        }

        Console.WriteLine($"[CreateChangedTimestampColumn] Successfully migrated ChangedDate to ChangedTimestamp for '{schema}.{table}'");
        return true;
    }

    private static async Task<bool> CreateCreatedTimestampColumnAsync(string schema, string table)
    {
        var sql = $"""
                   ALTER TABLE [{schema}].[{table}]
                   ADD [CreatedTimestamp] DATETIMEOFFSET NULL;

                   UPDATE [{schema}].[{table}]
                   SET [CreatedTimestamp] = [CreatedDate]
                   WHERE [CreatedTimestamp] IS NULL;

                   ALTER TABLE [{schema}].[{table}]
                   DROP COLUMN [CreatedDate];
                   """;

        if (string.IsNullOrWhiteSpace(MotoConfig.SqlConnectionString))
        {
            Console.WriteLine($"[CreateCreatedTimestampColumn] No connection string found for schema '{schema}'");
            return false;
        }

        foreach (var ts in sql.Split([";"], StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                await using var conn = new SqlConnection(MotoConfig.SqlConnectionString);
                await using var cmd = new SqlCommand(ts.Trim(), conn);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException e) when (e.Message.Contains("Column names in each table must be unique",
                                             StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"[CreateCreatedTimestampColumn] {e.Message}");
                return false;
            }
        }

        Console.WriteLine($"[CreateCreatedTimestampColumn] Successfully migrated CreatedDate to CreatedTimestamp for '{schema}.{table}'");
        return true;
    }
}
