namespace MotoRent.Domain.Core;

/// <summary>
/// Service for handling OAuth authentication and user mapping for SaaS signup.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user via Google.
    /// </summary>
    /// <param name="googleId">The unique Google Subject ID.</param>
    /// <param name="email">User's email address.</param>
    /// <param name="fullName">User's full name.</param>
    /// <returns>The authenticated user, or null if they need to complete signup.</returns>
    Task<User?> AuthenticateGoogleAsync(string googleId, string email, string fullName);

    /// <summary>
    /// Authenticates a user via LINE.
    /// </summary>
    /// <param name="lineId">The unique LINE User ID.</param>
    /// <param name="displayName">User's display name.</param>
    /// <returns>The authenticated user, or null if they need to complete signup.</returns>
    Task<User?> AuthenticateLineAsync(string lineId, string? displayName);
}
