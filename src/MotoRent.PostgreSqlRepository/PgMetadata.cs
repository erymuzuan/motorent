using System.Collections.Concurrent;
using MotoRent.Domain.Core;
using Npgsql;

namespace MotoRent.PostgreSqlRepository;

public class PgMetadata(IRequestContext context) : IPgMetadata
{
    private IRequestContext Context { get; } = context;
    private readonly ConcurrentDictionary<string, PgTable> m_cache = new();

    public PgTable GetTable(string name)
    {
        if (this.m_cache.TryGetValue(name, out var tb))
            return tb;

        var connectionString = this.Context.GetConnectionString();

        const string SQL = """
                           SELECT
                               column_name,
                               data_type,
                               COALESCE(character_maximum_length, 0),
                               is_nullable = 'YES',
                               is_identity = 'YES',
                               is_generated <> 'NEVER',
                               COALESCE(column_default, '')
                           FROM information_schema.columns
                           WHERE table_schema = 'public'
                             AND table_name = @table
                           ORDER BY ordinal_position
                           """;

        const string PK_SQL = """
                              SELECT kcu.column_name
                              FROM information_schema.table_constraints tc
                              JOIN information_schema.key_column_usage kcu
                                ON tc.constraint_name = kcu.constraint_name
                                AND tc.table_schema = kcu.table_schema
                              WHERE tc.constraint_type = 'PRIMARY KEY'
                                AND tc.table_schema = 'public'
                                AND tc.table_name = @table
                              """;

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        // Get primary key columns
        var primaryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var pkCmd = new NpgsqlCommand(PK_SQL, conn))
        {
            pkCmd.Parameters.AddWithValue("@table", name);
            using var pkReader = pkCmd.ExecuteReader();
            while (pkReader.Read())
            {
                primaryKeys.Add(pkReader.GetString(0));
            }
        }

        // Get all columns
        using var cmd = new NpgsqlCommand(SQL, conn);
        cmd.Parameters.AddWithValue("@table", name);
        using var reader = cmd.ExecuteReader();

        var table = new PgTable { Name = name };
        var columns = new List<PgColumn>();
        while (reader.Read())
        {
            var columnName = reader.GetString(0);
            var identity = reader.GetBoolean(4);
            var generated = reader.GetBoolean(5);
            var columnDefault = reader.GetString(6);
            var isPrimaryKey = primaryKeys.Contains(columnName);
            var hasSequenceDefault = isPrimaryKey && columnDefault.Contains("nextval");
            var readOnly = identity || generated || hasSequenceDefault;

            // Exclude tenant_id from writable columns since INSERT handles it separately
            var isTenantId = string.Equals(columnName, "tenant_id", StringComparison.OrdinalIgnoreCase);

            var column = new PgColumn
            {
                Name = columnName,
                SqlType = reader.GetString(1),
                Length = reader.GetInt32(2),
                IsNullable = reader.GetBoolean(3),
                IsIdentity = identity,
                IsPrimaryKey = isPrimaryKey,
                CanWrite = !readOnly && !isTenantId
            };
            columns.Add(column);
        }

        if (columns.Count(c => c.IsPrimaryKey) != 1)
            throw new InvalidOperationException(name + " contains " + columns.Count(c => c.IsPrimaryKey) + " primary keys");

        table.Columns = columns.ToArray();
        this.m_cache[name] = table;

        return this.GetTable(name);
    }
}
