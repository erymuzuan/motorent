using MotoRent.Domain.Core;
using Npgsql;

namespace MotoRent.PostgreSqlRepository;

public class DbConnectionInterceptor(IRequestContext context)
{
    private IRequestContext Context { get; } = context;

    public async Task SetTenantAsync(NpgsqlConnection connection)
    {
        var tenant = this.Context.GetAccountNo();
        if (string.IsNullOrWhiteSpace(tenant))
            throw new InvalidOperationException("Tenant account number is not available in the current request context");

        await using var cmd = new NpgsqlCommand("SELECT set_config('app.current_tenant', @tenant, false)", connection);
        cmd.Parameters.AddWithValue("@tenant", tenant);
        await cmd.ExecuteNonQueryAsync();
    }
}
