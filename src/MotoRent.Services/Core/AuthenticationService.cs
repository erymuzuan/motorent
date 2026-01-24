using MotoRent.Domain.Core;

namespace MotoRent.Services.Core;

/// <summary>
/// Implementation of IAuthenticationService for handling OAuth authentication.
/// </summary>
public class AuthenticationService(IDirectoryService directoryService) : IAuthenticationService
{
    private IDirectoryService DirectoryService { get; } = directoryService;

    public async Task<User?> AuthenticateGoogleAsync(string googleId, string email, string fullName)
    {
        // 1. Try to find user by explicit GoogleId
        var user = await this.DirectoryService.GetUserByProviderIdAsync(User.GOOGLE, googleId);
        if (user != null)
        {
            return user;
        }

        // 2. Try to find user by Email
        user = await this.DirectoryService.GetUserAsync(email.ToLowerInvariant());
        if (user != null)
        {
            // Link GoogleId to existing user
            user.GoogleId = googleId;
            if (string.IsNullOrWhiteSpace(user.NameIdentifier))
            {
                user.NameIdentifier = googleId;
                user.CredentialProvider = User.GOOGLE;
            }
            await this.DirectoryService.SaveUserProfileAsync(user);
            return user;
        }

        // 3. User not found - return null to indicate signup required
        return null;
    }

    public async Task<User?> AuthenticateLineAsync(string lineId, string? displayName)
    {
        // 1. Try to find user by explicit LineId
        var user = await this.DirectoryService.GetUserByProviderIdAsync(User.LINE, lineId);
        if (user != null)
        {
            return user;
        }

        // 2. User not found - return null to indicate signup required
        // Note: LINE users might not have email, so we typically use LINE ID as username if they sign up.
        return null;
    }
}
