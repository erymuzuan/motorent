using System.Security.Claims;

namespace MotoRent.Domain.Core;

/// <summary>
/// Authentication status result.
/// </summary>
public enum UserAuthenticationStatus
{
    /// <summary>
    /// User authenticated successfully.
    /// </summary>
    Authenticated,

    /// <summary>
    /// Invalid credentials.
    /// </summary>
    Unauthenticated,

    /// <summary>
    /// User account is locked.
    /// </summary>
    Locked,

    /// <summary>
    /// Password has expired.
    /// </summary>
    PasswordExpired,

    /// <summary>
    /// User not found.
    /// </summary>
    NotFound
}

/// <summary>
/// Service for user authentication, authorization, and directory operations.
/// </summary>
public interface IDirectoryService
{
    #region User Management

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    Task<User?> GetUserAsync(string userName);

    /// <summary>
    /// Gets a user by OAuth provider and name identifier.
    /// Used for providers that may not provide email (e.g., LINE).
    /// </summary>
    Task<User?> GetUserByProviderIdAsync(string provider, string nameIdentifier);

    /// <summary>
    /// Gets all users for an organization.
    /// </summary>
    Task<IEnumerable<User>> GetUsersAsync(string accountNo);

    /// <summary>
    /// Gets users in a specific role for an organization.
    /// </summary>
    Task<IEnumerable<User>> GetUsersInRoleAsync(string accountNo, string role);

    /// <summary>
    /// Saves/updates a user profile.
    /// </summary>
    Task SaveUserProfileAsync(User user);

    /// <summary>
    /// Authenticates a user with custom credentials.
    /// </summary>
    Task<UserAuthenticationStatus> AuthenticateAsync(string userName, string password);

    #endregion

    #region Organization Management

    /// <summary>
    /// Gets an organization by AccountNo.
    /// </summary>
    Task<Organization?> GetOrganizationAsync(string accountNo);

    /// <summary>
    /// Gets all organizations.
    /// </summary>
    Task<IEnumerable<Organization>> GetOrganizationsAsync();

    /// <summary>
    /// Saves/updates an organization.
    /// </summary>
    Task SaveOrganizationAsync(Organization organization);

    #endregion

    #region Claims and Tokens

    /// <summary>
    /// Gets the AccountNo for a user.
    /// </summary>
    Task<string?> GetAccountNoAsync(string userName);

    /// <summary>
    /// Gets subscriptions for a user's organization.
    /// </summary>
    Task<string[]> GetSubscriptionsAsync(string userName);

    /// <summary>
    /// Builds claims for a user in a specific account/organization.
    /// If account is null, returns super admin claims only.
    /// </summary>
    Task<IEnumerable<Claim>> GetClaimsAsync(string userName, string? account);

    /// <summary>
    /// Creates a JWT token for API access.
    /// </summary>
    Task<string> CreateJwtTokenAsync(string userName, string account);

    /// <summary>
    /// Reads and validates a JWT token, returning the claims.
    /// </summary>
    Task<IEnumerable<Claim>> ReadJwtTokenAsync(string jwt);

    #endregion

    #region Password Management

    /// <summary>
    /// Hashes a password with a salt.
    /// </summary>
    string HashPassword(string password, string salt);

    /// <summary>
    /// Generates a random salt for password hashing.
    /// </summary>
    string GenerateSalt();

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    bool VerifyPassword(string password, string salt, string hashedPassword);

    #endregion
}
