namespace MotoRent.Domain.Core;

/// <summary>
/// Progress callback for long-running operations like schema creation.
/// </summary>
public class ProvisioningProgress
{
    public string Message { get; set; } = "";
    public ProgressStatus Status { get; set; } = ProgressStatus.InProgress;
    public string? Group { get; set; }
}

public enum ProgressStatus
{
    InProgress,
    Done,
    Error
}

/// <summary>
/// Service for managing tenant subscriptions and schema provisioning.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Gets the current organization for the authenticated user.
    /// </summary>
    Task<Organization?> GetOrgAsync();

    /// <summary>
    /// Gets all organizations (for SuperAdmin).
    /// </summary>
    Task<IEnumerable<Organization>> GetAllOrganizationsAsync();

    /// <summary>
    /// Creates a new tenant schema with all required tables.
    /// </summary>
    /// <param name="accountNo">The tenant's AccountNo (schema name).</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    Task CreateSchemaAsync(string accountNo, Action<ProvisioningProgress>? progressCallback = null);

    /// <summary>
    /// Deletes a tenant schema and all associated data.
    /// Also removes users who only belong to this organization.
    /// </summary>
    /// <param name="organization">The organization to delete.</param>
    Task DeleteSchemaAsync(Organization organization);

    /// <summary>
    /// Creates and saves a new organization with its schema.
    /// </summary>
    /// <param name="organization">The organization to create.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    Task<Organization> CreateOrganizationAsync(Organization organization, Action<ProvisioningProgress>? progressCallback = null);

    /// <summary>
    /// Validates and generates a valid AccountNo from a suggested value.
    /// </summary>
    /// <param name="suggestedNo">The suggested AccountNo.</param>
    /// <returns>A valid AccountNo (sanitized if needed).</returns>
    Task<string> CreateAccountNoAsync(string suggestedNo);

    /// <summary>
    /// Checks if an AccountNo is available (not already used).
    /// </summary>
    /// <param name="accountNo">The AccountNo to check.</param>
    Task<bool> IsAccountNoAvailableAsync(string accountNo);

    /// <summary>
    /// Gets all users belonging to a specific organization.
    /// </summary>
    Task<IEnumerable<User>> GetUsersAsync(string accountNo);

    /// <summary>
    /// Adds a user to an organization with specified roles.
    /// </summary>
    Task AddUserToOrganizationAsync(User user, string accountNo, params string[] roles);

    /// <summary>
    /// Removes a user from an organization.
    /// If the user has no other organizations, the user is deleted.
    /// </summary>
    Task RemoveUserFromOrganizationAsync(User user, string accountNo);
}
