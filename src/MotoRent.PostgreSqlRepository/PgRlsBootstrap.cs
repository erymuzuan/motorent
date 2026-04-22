using Npgsql;

namespace MotoRent.PostgreSqlRepository;

public static class PgRlsBootstrap
{
    public static async Task EnsureRlsAsync(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("[RLS] No connection string configured, skipping RLS bootstrap");
            return;
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await EnsureAppRoleAsync(conn);

        var tables = await GetTenantTablesAsync(conn);
        var repaired = 0;
        foreach (var table in tables)
        {
            var changed = await EnsureTableRlsAsync(conn, table);
            if (changed) repaired++;
        }

        if (repaired > 0)
            Console.WriteLine($"[RLS] Fixed RLS on {repaired} of {tables.Count} tenant table(s)");
        else
            Console.WriteLine($"[RLS] All {tables.Count} tenant table(s) have RLS enforced");
    }

    private static async Task EnsureAppRoleAsync(NpgsqlConnection conn)
    {
        Console.WriteLine("[RLS] Ensuring motorent_app role and grants...");

        const string ensureRoleSql = """
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'motorent_app') THEN
                    CREATE ROLE motorent_app NOLOGIN NOINHERIT;
                END IF;
            END
            $$;
            """;

        const string grantsSql = """
            GRANT USAGE ON SCHEMA public TO motorent_app;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO motorent_app;
            GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO motorent_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO motorent_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO motorent_app;
            """;

        await using (var cmd = new NpgsqlCommand(ensureRoleSql, conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var grantsCmd = new NpgsqlCommand(grantsSql, conn))
        {
            await grantsCmd.ExecuteNonQueryAsync();
        }

        // Grant motorent_app to CURRENT_USER so the app (typically connecting as postgres) can SET ROLE motorent_app.
        try
        {
            await using var grantCmd = new NpgsqlCommand("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_auth_members
                        WHERE roleid = (SELECT oid FROM pg_roles WHERE rolname = 'motorent_app')
                          AND member = (SELECT oid FROM pg_roles WHERE rolname = CURRENT_USER)
                    ) THEN
                        EXECUTE 'GRANT motorent_app TO ' || quote_ident(CURRENT_USER);
                    END IF;
                END
                $$;
                """, conn);
            await grantCmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState is "42501" or "0LP01")
        {
            Console.WriteLine($"[RLS] Skipping GRANT motorent_app TO CURRENT_USER ({ex.MessageText})");
        }

        Console.WriteLine("[RLS] motorent_app role is ready");
    }

    private static async Task<List<string>> GetTenantTablesAsync(NpgsqlConnection conn)
    {
        const string sql = """
            SELECT table_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND column_name = 'tenant_id'
            ORDER BY table_name
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));
        return tables;
    }

    private static async Task<bool> EnsureTableRlsAsync(NpgsqlConnection conn, string table)
    {
        var changed = false;

        bool rlsEnabled = false, forceRls = false;
        await using (var checkCmd = new NpgsqlCommand("""
            SELECT relrowsecurity, relforcerowsecurity
            FROM pg_class
            WHERE relname = @table AND relnamespace = 'public'::regnamespace
            """, conn))
        {
            checkCmd.Parameters.AddWithValue("@table", table);
            await using var reader = await checkCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                rlsEnabled = reader.GetBoolean(0);
                forceRls = reader.GetBoolean(1);
            }
        }

        if (!rlsEnabled)
        {
            Console.WriteLine($"[RLS] Enabling RLS on \"{table}\"");
            await using var enableCmd = new NpgsqlCommand(
                $"""ALTER TABLE "{table}" ENABLE ROW LEVEL SECURITY""", conn);
            await enableCmd.ExecuteNonQueryAsync();
            changed = true;
        }

        object? policyExists;
        await using (var policyCmd = new NpgsqlCommand("""
            SELECT 1 FROM pg_policies
            WHERE schemaname = 'public' AND tablename = @table
              AND policyname LIKE 'tenant_isolation%'
            """, conn))
        {
            policyCmd.Parameters.AddWithValue("@table", table);
            policyExists = await policyCmd.ExecuteScalarAsync();
        }

        if (policyExists is null)
        {
            var policyName = $"tenant_isolation_{table.ToLowerInvariant()}";
            Console.WriteLine($"[RLS] Creating policy \"{policyName}\" on \"{table}\"");
            await using var createCmd = new NpgsqlCommand(
                $"""CREATE POLICY "{policyName}" ON "{table}" USING ("tenant_id" = current_setting('app.current_tenant'))""",
                conn);
            await createCmd.ExecuteNonQueryAsync();
            changed = true;
        }

        if (!forceRls)
        {
            Console.WriteLine($"[RLS] Forcing RLS on \"{table}\"");
            await using var forceCmd = new NpgsqlCommand(
                $"""ALTER TABLE "{table}" FORCE ROW LEVEL SECURITY""", conn);
            await forceCmd.ExecuteNonQueryAsync();
            changed = true;
        }

        await using (var grantCmd = new NpgsqlCommand(
            $"""GRANT SELECT, INSERT, UPDATE, DELETE ON "{table}" TO motorent_app""", conn))
        {
            await grantCmd.ExecuteNonQueryAsync();
        }

        return changed;
    }
}
