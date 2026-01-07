namespace MotoRent.Domain.Core;

/// <summary>
/// Links a User to an Organization (tenant) with specific roles.
/// A user can have multiple UserAccounts for different organizations.
/// </summary>
public class UserAccount
{
    // Role constants for MotoRent
    public const string SUPER_ADMIN = "administrator";
    public const string ORG_ADMIN = "OrgAdmin";
    public const string SHOP_MANAGER = "ShopManager";
    public const string STAFF = "Staff";
    public const string MECHANIC = "Mechanic";
    public const string REGISTERED_USER = "RegisteredUser";

    // Policy constants
    public const string POLICY_SUPER_ADMIN_IMPERSONATE = "POLICY_SUPER_ADMIN_IMPERSONATE";

    /// <summary>
    /// All available roles for assignment (excluding SUPER_ADMIN which is system-level).
    /// </summary>
    public static readonly string[] AllRoles =
    [
        ORG_ADMIN,
        SHOP_MANAGER,
        STAFF,
        MECHANIC
    ];

    /// <summary>
    /// Roles that can manage shop operations.
    /// </summary>
    public static readonly string[] ManagementRoles =
    [
        ORG_ADMIN,
        SHOP_MANAGER
    ];

    /// <summary>
    /// Roles that can perform rentals.
    /// </summary>
    public static readonly string[] RentalRoles =
    [
        ORG_ADMIN,
        SHOP_MANAGER,
        STAFF
    ];

    /// <summary>
    /// Roles that can perform maintenance.
    /// </summary>
    public static readonly string[] MaintenanceRoles =
    [
        ORG_ADMIN,
        SHOP_MANAGER,
        MECHANIC
    ];

    /// <summary>
    /// The organization's AccountNo this user account belongs to.
    /// </summary>
    public string AccountNo { get; set; } = "";

    /// <summary>
    /// Default start page for this account.
    /// </summary>
    public string? StartPage { get; set; }

    /// <summary>
    /// Whether this is the user's favourite/default account.
    /// </summary>
    public bool IsFavourite { get; set; }

    /// <summary>
    /// Roles assigned to the user for this organization.
    /// </summary>
    public List<string> Roles { get; } = [];

    public override string ToString() => AccountNo;
}
