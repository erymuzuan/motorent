using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Represents a user in the system. Users can belong to multiple organizations
/// via their AccountCollection.
/// </summary>
public class User : Entity
{
    // Credential provider constants
    public const string CUSTOM = "Custom";
    public const string GOOGLE = "Google";
    public const string MICROSOFT = "Microsoft";
    public const string LINE = "Line";

    public int UserId { get; set; }

    /// <summary>
    /// Unique username, typically the email address.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = "";

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = "";

    /// <summary>
    /// Optional nickname or display name.
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// User's phone number.
    /// </summary>
    public string? Telephone { get; set; }

    /// <summary>
    /// Profile photo store ID.
    /// </summary>
    public string? PhotoStoreId { get; set; }

    /// <summary>
    /// UI theme preference.
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Language/locale preference.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Authentication provider (Custom, Google, Microsoft).
    /// </summary>
    public string CredentialProvider { get; set; } = GOOGLE;

    /// <summary>
    /// Whether the user account is locked out.
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Whether the email has been verified for custom credentials.
    /// </summary>
    public bool CustomEmailVerified { get; set; }

    /// <summary>
    /// Salt for password hashing (custom credentials only).
    /// </summary>
    public string? Salt { get; set; }

    /// <summary>
    /// Hashed password (custom credentials only).
    /// </summary>
    public string? HashedPassword { get; set; }

    /// <summary>
    /// Name identifier from OAuth provider.
    /// </summary>
    public string? NameIdentifier { get; set; }

    /// <summary>
    /// Collection of accounts (organizations) this user belongs to.
    /// </summary>
    public List<UserAccount> AccountCollection { get; } = [];

    /// <summary>
    /// Gets the default AccountNo from the first account.
    /// </summary>
    public string? AccountNo => AccountCollection.FirstOrDefault()?.AccountNo;

    /// <summary>
    /// Checks if the user has a specific role in a given account.
    /// </summary>
    public bool HasRole(string accountNo, params string[] roles)
    {
        var account = AccountCollection.FirstOrDefault(a => a.AccountNo == accountNo);
        if (account == null) return false;
        return roles.Any(r => account.Roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the roles for a specific account.
    /// </summary>
    public IEnumerable<string> GetRoles(string accountNo)
    {
        var account = AccountCollection.FirstOrDefault(a => a.AccountNo == accountNo);
        return account?.Roles ?? [];
    }

    public override int GetId() => UserId;
    public override void SetId(int value) => UserId = value;

    public override string ToString() => $"{UserName} ({FullName})";
}
