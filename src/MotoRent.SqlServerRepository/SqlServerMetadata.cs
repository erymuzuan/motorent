using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;

namespace MotoRent.SqlServerRepository;

public class SqlServerMetadata : ISqlServerMetadata
{
    private readonly IRequestContext m_context;
    private readonly ConcurrentDictionary<string, Table> m_cache = new();

    public SqlServerMetadata(IRequestContext context)
    {
        m_context = context;
    }

    public void RemoveCache(string tableName) => m_cache.TryRemove(tableName, out _);

    public async Task<Table?> GetTableAsync(string account, string name)
    {
        var key = $"{account}.{name}";
        if (m_cache.TryGetValue(key, out var tb))
            return tb;

        return await GetTableAsync2(name, account);
    }

    private async Task<Table?> GetTableAsync2(string name, string accountNo, int recurse = 0)
    {
        var sql = $"""
                   SELECT
                           '[' + s.name + '].[' + o.name + ']' as 'Table'
                           ,c.name as 'Column'
                           ,t.name as 'Type'
                           ,c.max_length as 'length'
                           ,c.is_nullable as 'IsNullable'
                   	    ,c.is_identity as 'IsIdentity'
                   		,c.is_computed as 'Computed'
                       FROM
                           sys.objects o INNER JOIN sys.all_columns c
                           ON c.object_id = o.object_id
                           INNER JOIN sys.types t
                           ON c.system_type_id = t.system_type_id
                           INNER JOIN sys.schemas s
                           ON s.schema_id = o.schema_id
                       WHERE
                           o.type = 'U'
                   		AND t.name <> 'sysname'
                           AND s.name = N'{accountNo}'
                           AND o.Name = @Table
                       ORDER
                           BY o.type
                   """;

        var connectionString = m_context.GetConnectionString();
        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Table", name);
        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        var table = new Table { Name = name };
        var columns = new List<Column>();
        while (await reader.ReadAsync())
        {
            var identity = reader.GetBoolean(5);
            var computed = reader.GetBoolean(6);
            var readOnly = identity || computed;
            var column = new Column
            {
                Name = reader.GetString(1),
                SqlType = reader.GetString(2),
                Length = reader.GetInt16(3),
                IsNullable = reader.GetBoolean(4),
                IsIdentity = reader.GetBoolean(5),
                CanWrite = !readOnly
            };
            column.IsPrimaryKey = column.IsIdentity;
            columns.Add(column);
        }

        if (columns is [])
            return null; // Table doesn't exist

        var pk = columns.Count(c => c.IsPrimaryKey);
        if (pk is not 1 && recurse < 5)
        {
            await Task.Delay(400);
            return await GetTableAsync2(name, accountNo, ++recurse);
        }

        if (pk != 1 && recurse >= 5)
            throw new InvalidOperationException($"[{accountNo}].[{name}] contains {pk} primary keys");

        table.Columns = columns.ToArray();
        var key = $"{accountNo}.{name}";
        m_cache.TryAdd(key, table);
        return table;
    }
}
