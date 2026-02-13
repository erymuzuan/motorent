using System.Text.RegularExpressions;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using Npgsql;

namespace MotoRent.Services.Core;

/// <summary>
/// PostgreSQL-based service for managing tenant subscriptions and provisioning.
/// Uses RLS (Row Level Security) with tenant_id instead of SQL Server schemas.
/// Tables are shared across tenants; isolation is enforced by tenant_id column + RLS policies.
/// </summary>
public partial class SqlSubscriptionService(
    CoreDataContext context,
    IDirectoryService directoryService,
    IRequestContext requestContext) : ISubscriptionService
{
    // Tables that should be created first (no dependencies or are depended upon by others)
    private static readonly HashSet<string> s_priorityTables =
    [
        "MotoRent.Shop.sql",
        "MotoRent.Renter.sql",
        "MotoRent.Document.sql",
        "MotoRent.ServiceType.sql",
        "MotoRent.VehicleOwner.sql",
        "MotoRent.VehiclePool.sql",
        "MotoRent.Agent.sql",
        "MotoRent.DenominationGroup.sql",
        "MotoRent.DenominationRate.sql",
        "MotoRent.ExchangeRate.sql",
        "MotoRent.DocumentTemplate.sql",
        "MotoRent.ServiceLocation.sql",
        "MotoRent.PricingRule.sql",
        "MotoRent.RateDelta.sql"
    ];

    // Private get properties from constructor injection
    private CoreDataContext Context { get; } = context;
    private IDirectoryService DirectoryService { get; } = directoryService;
    private IRequestContext RequestContext { get; } = requestContext;

    #region Organization Management

    public async Task<Organization?> GetOrgAsync()
    {
        var accountNo = await this.RequestContext.GetAccountNoAsync();
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

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization saved. Provisioning tenant [{organization.AccountNo}]..." });

        // Provision tenant tables (create if not exist, ensure RLS policies)
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

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Organization saved. Provisioning tenant [{organization.AccountNo}]..." });

        // Provision tenant tables
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
        // Convert to PascalCase: split by non-alphanumeric, capitalize each word
        var words = PascalCaseSplitRegex().Split(suggestedNo)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(ToPascalCaseWord);

        var pascalCased = string.Concat(words);

        // Remove any remaining non-ASCII alphanumeric characters
        var sanitized = AccountNoRegex().Replace(pascalCased, "");

        // If all characters were invalid, generate a new one
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return Task.FromResult("Tenant" + Guid.NewGuid().ToString("N")[..8]);
        }

        // Ensure it doesn't start with a number
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "Org" + sanitized;
        }

        // Limit length to 50 characters
        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50];
        }

        return Task.FromResult(sanitized);
    }

    private static string ToPascalCaseWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return "";

        // Keep only ASCII letters and digits
        var chars = word.Where(c => char.IsAsciiLetterOrDigit(c)).ToArray();
        if (chars.Length == 0) return "";

        // If word already has mixed case (PascalCase), preserve it
        var hasUpperAfterFirst = chars.Skip(1).Any(char.IsUpper);
        var hasLower = chars.Any(char.IsLower);
        if (hasUpperAfterFirst && hasLower)
        {
            chars[0] = char.ToUpperInvariant(chars[0]);
            return new string(chars);
        }

        // Capitalize first letter, lowercase the rest
        chars[0] = char.ToUpperInvariant(chars[0]);
        for (var i = 1; i < chars.Length; i++)
        {
            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]+")]
    private static partial Regex PascalCaseSplitRegex();

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex AccountNoRegex();

    #endregion

    #region Schema Management

    /// <summary>
    /// In PostgreSQL with RLS, "creating a schema" means ensuring tables exist and RLS policies are in place.
    /// Tables are shared across all tenants; tenant isolation is enforced by tenant_id + RLS policies.
    /// This executes table creation scripts (IF NOT EXISTS) from the database folder.
    /// </summary>
    public async Task CreateSchemaAsync(string accountNo, Action<ProvisioningProgress>? progressCallback = null)
    {
        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Provisioning tenant [{accountNo}]..." });

        var connectionString = MotoConfig.SqlConnectionString;

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

        // Dynamically discover all table files (MotoRent.*.sql, excluding .seed.sql)
        var tableFiles = Directory.GetFiles(tablesFolder, "MotoRent.*.sql")
            .Select(Path.GetFileName)
            .Where(f => f != null && !f.Contains(".seed.", StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .OrderBy(f => s_priorityTables.Contains(f) ? 0 : 1) // Priority tables first
            .ThenBy(f => f) // Then alphabetically
            .ToList();

        progressCallback?.Invoke(new ProvisioningProgress { Message = $"Found {tableFiles.Count} table scripts to execute..." });

        foreach (var tableFile in tableFiles)
        {
            var filePath = Path.Combine(tablesFolder, tableFile);

            var sqlTemplate = await File.ReadAllTextAsync(filePath);

            // For PostgreSQL, table scripts use CREATE TABLE IF NOT EXISTS
            // No schema replacement needed - tables are in public schema with tenant_id
            var sql = sqlTemplate;

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (NpgsqlException ex) when (ex.SqlState == "42P07") // table already exists
            {
                // Silently skip - table already exists
            }
            catch (NpgsqlException ex)
            {
                // Log but continue
                progressCallback?.Invoke(new ProvisioningProgress
                {
                    Message = $"Warning ({tableFile}): {ex.Message}",
                    Status = ProgressStatus.InProgress
                });
            }

            progressCallback?.Invoke(new ProvisioningProgress
            {
                Message = $"Created table from {tableFile}",
                Status = ProgressStatus.InProgress
            });
        }

        progressCallback?.Invoke(new ProvisioningProgress
        {
            Message = $"Tenant [{accountNo}] provisioned successfully!",
            Status = ProgressStatus.Done
        });
    }

    public async Task DeleteSchemaAsync(Organization organization)
    {
        var accountNo = organization.AccountNo;
        var connectionString = MotoConfig.SqlConnectionString;

        // In PostgreSQL with RLS, deleting a "schema" means removing all tenant data
        // Delete all rows with matching tenant_id from all tables

        // Get all tables that have tenant_id column
        var tablesSql = """
            SELECT table_name
            FROM information_schema.columns
            WHERE column_name = 'tenant_id'
            AND table_schema = 'public'
            """;

        var tables = new List<string>();

        await using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(tablesSql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        // Delete all data for this tenant from each table
        foreach (var table in tables)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand($"DELETE FROM \"{table}\" WHERE \"tenant_id\" = @tenantId", conn);
                cmd.Parameters.AddWithValue("@tenantId", accountNo);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (NpgsqlException)
            {
                // Continue even if a table delete fails
            }
        }

        // Get all users in this organization
        var users = (await this.GetUsersAsync(accountNo)).ToList();

        // Remove users who only belong to this organization
        foreach (var user in users.Where(u => u.AccountCollection.Count == 1))
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DELETE FROM \"User\" WHERE \"UserId\" = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", user.UserId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Remove this account from users who belong to multiple organizations
        foreach (var user in users.Where(u => u.AccountCollection.Count > 1))
        {
            user.AccountCollection.RemoveAll(a => a.AccountNo == accountNo);
            await this.DirectoryService.SaveUserProfileAsync(user);
        }

        // Delete the organization
        await using (var conn = new NpgsqlConnection(connectionString))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DELETE FROM \"Organization\" WHERE \"OrganizationId\" = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", organization.OrganizationId);
            await cmd.ExecuteNonQueryAsync();
        }
    }

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
        var connectionString = MotoConfig.SqlConnectionString;

        user.AccountCollection.RemoveAll(a => a.AccountNo == accountNo);

        if (user.AccountCollection.Count == 0)
        {
            // Delete the user entirely
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DELETE FROM \"User\" WHERE \"UserId\" = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", user.UserId);
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            await this.DirectoryService.SaveUserProfileAsync(user);
        }
    }

    #endregion
}
