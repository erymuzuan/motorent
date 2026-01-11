using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Core;

/// <summary>
/// SQL-based service for managing tenant subscriptions and schema provisioning.
/// </summary>
public partial class SqlSubscriptionService : ISubscriptionService
{
    private readonly CoreDataContext m_context;
    private readonly IDirectoryService m_directoryService;
    private readonly IRequestContext m_requestContext;
    private readonly QueryProvider m_queryProvider;

    public SqlSubscriptionService(
        CoreDataContext context,
        IDirectoryService directoryService,
        IRequestContext requestContext,
        QueryProvider queryProvider)
    {
        m_context = context;
        m_directoryService = directoryService;
        m_requestContext = requestContext;
        m_queryProvider = queryProvider;
    }

    #region Organization Management

    public async Task<Organization?> GetOrgAsync()
    {
        var accountNo = m_requestContext.GetAccountNo();
        if (string.IsNullOrWhiteSpace(accountNo)) return null;

        return await m_directoryService.GetOrganizationAsync(accountNo);
    }

    public async Task<IEnumerable<Organization>> GetAllOrganizationsAsync()
    {
        var query = m_context.Organizations.OrderBy(o => o.Name);
        var result = await m_context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<Organization> CreateOrganizationAsync(Organization organization, Action<ProvisioningProgress>? progressCallback = null)
    {
        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Creating organization {organization.Name}..." });

        // Save the organization first
        using var session = m_context.OpenSession("system");
        session.Attach(organization);
        await session.SubmitChanges("CreateOrganization");

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization saved. Creating schema [{organization.AccountNo}]..." });

        // Create the schema and tables
        await CreateSchemaAsync(organization.AccountNo, progressCallback);

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization {organization.Name} created successfully!", Status = ProgressStatus.Done });

        return organization;
    }

    public async Task<bool> IsAccountNoAvailableAsync(string accountNo)
    {
        var existing = await m_directoryService.GetOrganizationAsync(accountNo);
        return existing == null;
    }

    public Task<string> CreateAccountNoAsync(string suggestedNo)
    {
        // Remove invalid characters, keep only alphanumeric and underscore
        var sanitized = AccountNoRegex().Replace(suggestedNo, "");

        // If all characters were invalid, generate a new one
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return Task.FromResult("Tenant" + Guid.NewGuid().ToString("N")[..8]);
        }

        // Ensure it doesn't start with a number
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "T" + sanitized;
        }

        // Limit length to 50 characters
        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50];
        }

        return Task.FromResult(sanitized);
    }

    [GeneratedRegex("[^a-zA-Z0-9_]")]
    private static partial Regex AccountNoRegex();

    #endregion

    #region Schema Management

    public async Task CreateSchemaAsync(string accountNo, Action<ProvisioningProgress>? progressCallback = null)
    {
        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Creating schema [{accountNo}]..." });

        // Create the schema
        try
        {
            var schemaSql = $"CREATE SCHEMA [{accountNo}] AUTHORIZATION [dbo]";
            await using var schemaConn = m_queryProvider.CreateConnection();
            await using var schemaCommand = new SqlCommand(schemaSql, schemaConn);
            await schemaConn.OpenAsync();
            await schemaCommand.ExecuteNonQueryAsync();
            progressCallback?.Invoke(new ProvisioningProgress { Message = $"Schema [{accountNo}] created." });
        }
        catch (SqlException ex) when (ex.Number == 2714) // Schema already exists
        {
            progressCallback?.Invoke(new ProvisioningProgress
            {
                Message = $"Schema [{accountNo}] already exists.",
                Status = ProgressStatus.InProgress
            });
        }
        catch (SqlException ex)
        {
            progressCallback?.Invoke(new ProvisioningProgress
            {
                Message = $"Error creating schema: {ex.Message}",
                Status = ProgressStatus.Error
            });
            throw;
        }

        // Read and execute the schema template
        var databaseFolder = MotoConfig.DatabaseSource;
        var schemaFile = Path.Combine(databaseFolder, "001-create-schema.sql");

        if (!File.Exists(schemaFile))
        {
            // Try relative to app base directory
            schemaFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "database", "001-create-schema.sql");
        }

        if (!File.Exists(schemaFile))
        {
            progressCallback?.Invoke(new ProvisioningProgress
            {
                Message = $"Schema template file not found: {schemaFile}",
                Status = ProgressStatus.Error
            });
            throw new FileNotFoundException("Schema template file not found", schemaFile);
        }

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Reading schema template from {schemaFile}..." });

        var sqlTemplate = await File.ReadAllTextAsync(schemaFile);

        // Replace placeholder with actual schema name
        var sql = sqlTemplate.Replace("<schema>", accountNo);

        // Split by GO statements and execute each batch
        var batches = SplitSqlBatches(sql);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;

            try
            {
                await using var conn = m_queryProvider.CreateConnection();
                await using var cmd = new SqlCommand(batch, conn);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // Log but continue (table might already exist)
                progressCallback?.Invoke(new ProvisioningProgress
                {
                    Message = $"Warning: {ex.Message}",
                    Status = ProgressStatus.InProgress
                });
            }
        }

        progressCallback?.Invoke(new ProvisioningProgress
        {
            Message = $"Schema [{accountNo}] tables created successfully!",
            Status = ProgressStatus.Done
        });
    }

    public async Task DeleteSchemaAsync(Organization organization)
    {
        var accountNo = organization.AccountNo;

        // Get all tables in the schema
        var tablesSql = $"""
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = '{accountNo}'
            """;

        var tables = new List<string>();

        await using (var conn = m_queryProvider.CreateConnection())
        {
            await using var cmd = new SqlCommand(tablesSql, conn);
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        // Drop all tables
        foreach (var table in tables)
        {
            await using var conn = m_queryProvider.CreateConnection();
            await using var cmd = new SqlCommand($"DROP TABLE [{accountNo}].[{table}]", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Drop the schema
        await using (var conn = m_queryProvider.CreateConnection())
        {
            await using var cmd = new SqlCommand($"DROP SCHEMA [{accountNo}]", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Get all users in this organization
        var users = (await GetUsersAsync(accountNo)).ToList();

        // Remove users who only belong to this organization
        foreach (var user in users.Where(u => u.AccountCollection.Count == 1))
        {
            await using var conn = m_queryProvider.CreateConnection();
            await using var cmd = new SqlCommand($"DELETE FROM [Core].[User] WHERE [UserId] = {user.UserId}", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Remove this account from users who belong to multiple organizations
        foreach (var user in users.Where(u => u.AccountCollection.Count > 1))
        {
            user.AccountCollection.RemoveAll(a => a.AccountNo == accountNo);
            await m_directoryService.SaveUserProfileAsync(user);
        }

        // Delete the organization
        await using (var conn = m_queryProvider.CreateConnection())
        {
            await using var cmd = new SqlCommand($"DELETE FROM [Core].[Organization] WHERE [OrganizationId] = {organization.OrganizationId}", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static IEnumerable<string> SplitSqlBatches(string sql)
    {
        // Split on GO statements (case insensitive, on its own line)
        return GoRegex().Split(sql)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s));
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex GoRegex();

    #endregion

    #region User Management

    public async Task<IEnumerable<User>> GetUsersAsync(string accountNo)
    {
        return await m_directoryService.GetUsersAsync(accountNo);
    }

    public async Task AddUserToOrganizationAsync(User user, string accountNo, params string[] roles)
    {
        var existingAccount = user.AccountCollection.FirstOrDefault(a => a.AccountNo == accountNo);

        if (existingAccount != null)
        {
            // Update roles
            existingAccount.Roles.Clear();
            existingAccount.Roles.AddRange(roles);
        }
        else
        {
            // Add new account
            user.AccountCollection.Add(new UserAccount
            {
                AccountNo = accountNo,
                Roles = { },
                IsFavourite = user.AccountCollection.Count == 0
            });
            user.AccountCollection.Last().Roles.AddRange(roles);
        }

        await m_directoryService.SaveUserProfileAsync(user);
    }

    public async Task RemoveUserFromOrganizationAsync(User user, string accountNo)
    {
        user.AccountCollection.RemoveAll(a => a.AccountNo == accountNo);

        if (user.AccountCollection.Count == 0)
        {
            // Delete the user entirely
            await using var conn = m_queryProvider.CreateConnection();
            await using var cmd = new SqlCommand($"DELETE FROM [Core].[User] WHERE [UserId] = {user.UserId}", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            await m_directoryService.SaveUserProfileAsync(user);
        }
    }

    #endregion
}
