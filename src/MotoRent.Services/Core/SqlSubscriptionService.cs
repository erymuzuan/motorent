using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Core;

/// <summary>
/// SQL-based service for managing tenant subscriptions and schema provisioning.
/// </summary>
public partial class SqlSubscriptionService(
    CoreDataContext context,
    IDirectoryService directoryService,
    IRequestContext requestContext,
    QueryProvider queryProvider) : ISubscriptionService
{
    // Static readonly for table creation order
    private static readonly string[] s_tableOrder =
    [
        "MotoRent.Shop.sql",
        "MotoRent.Renter.sql",
        "MotoRent.Document.sql",
        "MotoRent.Motorbike.sql",
        "MotoRent.Insurance.sql",
        "MotoRent.Accessory.sql",
        "MotoRent.ServiceType.sql",
        "MotoRent.MaintenanceSchedule.sql",
        "MotoRent.Rental.sql",
        "MotoRent.RentalAccessory.sql",
        "MotoRent.Deposit.sql",
        "MotoRent.Payment.sql",
        "MotoRent.DamageReport.sql",
        "MotoRent.DamagePhoto.sql",
        "MotoRent.RentalAgreement.sql"
    ];

    // Private get properties from constructor injection
    private CoreDataContext Context { get; } = context;
    private IDirectoryService DirectoryService { get; } = directoryService;
    private IRequestContext RequestContext { get; } = requestContext;
    private QueryProvider QueryProvider { get; } = queryProvider;

    #region Organization Management

    public async Task<Organization?> GetOrgAsync()
    {
        var accountNo = this.RequestContext.GetAccountNo();
        if (string.IsNullOrWhiteSpace(accountNo)) return null;

        return await this.DirectoryService.GetOrganizationAsync(accountNo);
    }

    public async Task<IEnumerable<Organization>> GetAllOrganizationsAsync()
    {
        var query = this.Context.Organizations.OrderBy(o => o.Name);
        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection;
    }

    public async Task<Organization> CreateOrganizationAsync(Organization organization, Action<ProvisioningProgress>? progressCallback = null)
    {
        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Creating organization {organization.Name}..." });

        // Save the organization first
        using var session = this.Context.OpenSession("system");
        session.Attach(organization);
        await session.SubmitChanges("CreateOrganization");

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization saved. Creating schema [{organization.AccountNo}]..." });

        // Create the schema and tables
        await this.CreateSchemaAsync(organization.AccountNo, progressCallback);

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization {organization.Name} created successfully!", Status = ProgressStatus.Done });

        return organization;
    }

    public async Task<Organization> CreateOrganizationWithAdminAsync(Organization organization, OrganizationAdminInfo adminInfo, Action<ProvisioningProgress>? progressCallback = null)
    {
        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Creating organization {organization.Name}..." });

        // Save the organization first
        using var session = this.Context.OpenSession("system");
        session.Attach(organization);
        await session.SubmitChanges("CreateOrganization");

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization saved. Creating schema [{organization.AccountNo}]..." });

        // Create the schema and tables
        await this.CreateSchemaAsync(organization.AccountNo, progressCallback);

        // Create or update the admin user
        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Setting up administrator {adminInfo.Email}..." });

        var existingUser = await this.DirectoryService.GetUserAsync(adminInfo.Email);

        if (existingUser != null)
        {
            // User exists, add them to this organization as OrgAdmin
            await this.AddUserToOrganizationAsync(existingUser, organization.AccountNo, UserAccount.ORG_ADMIN);
            progressCallback?.Invoke(new ProvisioningProgress { Message = $"Existing user {adminInfo.Email} added as administrator." });
        }
        else
        {
            // Create new user
            var newUser = new User
            {
                UserName = adminInfo.Email,
                Email = adminInfo.Email,
                FullName = adminInfo.FullName,
                CredentialProvider = adminInfo.Provider,
                Language = organization.Language
            };

            // Add the organization account with OrgAdmin role
            var userAccount = new UserAccount
            {
                AccountNo = organization.AccountNo,
                IsFavourite = true
            };
            userAccount.Roles.Add(UserAccount.ORG_ADMIN);
            newUser.AccountCollection.Add(userAccount);

            await this.DirectoryService.SaveUserProfileAsync(newUser);
            progressCallback?.Invoke(new ProvisioningProgress { Message = $"Administrator {adminInfo.Email} created successfully." });
        }

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization {organization.Name} created successfully!", Status = ProgressStatus.Done });

        return organization;
    }

    public async Task<bool> IsAccountNoAvailableAsync(string accountNo)
    {
        var existing = await this.DirectoryService.GetOrganizationAsync(accountNo);
        return existing is null;
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
            await using var schemaConn = this.QueryProvider.CreateConnection();
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

        // Read and execute table scripts from the tables folder
        var databaseFolder = MotoConfig.DatabaseSource;
        var tablesFolder = Path.Combine(databaseFolder, "tables");

        if (!Directory.Exists(tablesFolder))
        {
            // Try relative to app base directory
            tablesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "database", "tables");
        }

        if (!Directory.Exists(tablesFolder))
        {
            progressCallback?.Invoke(new ProvisioningProgress
            {
                Message = $"Tables folder not found: {tablesFolder}",
                Status = ProgressStatus.Error
            });
            throw new DirectoryNotFoundException($"Tables folder not found: {tablesFolder}");
        }

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Reading table scripts from {tablesFolder}..." });

        foreach (var tableFile in s_tableOrder)
        {
            var filePath = Path.Combine(tablesFolder, tableFile);
            if (!File.Exists(filePath))
            {
                progressCallback?.Invoke(new ProvisioningProgress
                {
                    Message = $"Skipping missing table file: {tableFile}",
                    Status = ProgressStatus.InProgress
                });
                continue;
            }

            var sqlTemplate = await File.ReadAllTextAsync(filePath);
            var sql = sqlTemplate.Replace("<schema>", accountNo);

            // Split by GO statements and execute each batch
            var batches = SplitSqlBatches(sql);

            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;

                try
                {
                    await using var conn = this.QueryProvider.CreateConnection();
                    await using var cmd = new SqlCommand(batch, conn);
                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (SqlException ex)
                {
                    // Log but continue (table might already exist)
                    progressCallback?.Invoke(new ProvisioningProgress
                    {
                        Message = $"Warning ({tableFile}): {ex.Message}",
                        Status = ProgressStatus.InProgress
                    });
                }
            }

            progressCallback?.Invoke(new ProvisioningProgress
            {
                Message = $"Created table from {tableFile}",
                Status = ProgressStatus.InProgress
            });
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

        await using (var conn = this.QueryProvider.CreateConnection())
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
            await using var conn = this.QueryProvider.CreateConnection();
            await using var cmd = new SqlCommand($"DROP TABLE [{accountNo}].[{table}]", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Drop the schema
        await using (var conn = this.QueryProvider.CreateConnection())
        {
            await using var cmd = new SqlCommand($"DROP SCHEMA [{accountNo}]", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Get all users in this organization
        var users = (await this.GetUsersAsync(accountNo)).ToList();

        // Remove users who only belong to this organization
        foreach (var user in users.Where(u => u.AccountCollection.Count == 1))
        {
            await using var conn = this.QueryProvider.CreateConnection();
            await using var cmd = new SqlCommand($"DELETE FROM [Core].[User] WHERE [UserId] = {user.UserId}", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Remove this account from users who belong to multiple organizations
        foreach (var user in users.Where(u => u.AccountCollection.Count > 1))
        {
            user.AccountCollection.RemoveAll(a => a.AccountNo == accountNo);
            await this.DirectoryService.SaveUserProfileAsync(user);
        }

        // Delete the organization
        await using (var conn = this.QueryProvider.CreateConnection())
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
        return await this.DirectoryService.GetUsersAsync(accountNo);
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

        await this.DirectoryService.SaveUserProfileAsync(user);
    }

    public async Task RemoveUserFromOrganizationAsync(User user, string accountNo)
    {
        user.AccountCollection.RemoveAll(a => a.AccountNo == accountNo);

        if (user.AccountCollection.Count == 0)
        {
            // Delete the user entirely
            await using var conn = this.QueryProvider.CreateConnection();
            await using var cmd = new SqlCommand($"DELETE FROM [Core].[User] WHERE [UserId] = {user.UserId}", conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            await this.DirectoryService.SaveUserProfileAsync(user);
        }
    }

    #endregion
}
