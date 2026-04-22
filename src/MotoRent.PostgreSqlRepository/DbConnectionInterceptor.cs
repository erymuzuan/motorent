using MotoRent.Domain.Core;
using Npgsql;

namespace MotoRent.PostgreSqlRepository;

public class DbConnectionInterceptor(IRequestContext context)
{
    private IRequestContext Context { get; } = context;

    public async Task SetTenantAsync(NpgsqlConnection connection)
    {
        var tenant = await this.Context.GetAccountNoAsync();
        if (string.IsNullOrWhiteSpace(tenant))
            throw new InvalidOperationException("Tenant account number is not available in the current request context");

        // Drop superuser privileges so FORCE RLS policies actually apply.
        await using (var roleCmd = new NpgsqlCommand("SET ROLE motorent_app", connection))
        {
            await roleCmd.ExecuteNonQueryAsync();
        }

        await using var cmd = new NpgsqlCommand("SELECT set_config('app.current_tenant', @tenant, false)", connection);
        cmd.Parameters.AddWithValue("@tenant", tenant);
        await cmd.ExecuteNonQueryAsync();
    }
}
