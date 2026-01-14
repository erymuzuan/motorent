using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Tracks pending email invitations that haven't been accepted yet.
/// When a user logs in via OAuth with a matching email, they are automatically
/// added to the organization with the specified roles.
/// </summary>
public class UserInvite : Entity
{
    public int UserInviteId { get; set; }

    /// <summary>
    /// Email address of the invited user (case-insensitive match on login).
    /// </summary>
    public string Email { get; set; } = "";

    /// <summary>
    /// Organization (tenant) to add the user to.
    /// </summary>
    public string AccountNo { get; set; } = "";

    /// <summary>
    /// Roles to assign when the user accepts the invitation.
    /// </summary>
    public List<string> Roles { get; } = [];

    /// <summary>
    /// Username of the person who created the invitation.
    /// </summary>
    public string InvitedBy { get; set; } = "";

    /// <summary>
    /// When the invitation expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Invitation status: Pending, Accepted, Cancelled, Expired.
    /// </summary>
    public string Status { get; set; } = STATUS_PENDING;

    public const string STATUS_PENDING = "Pending";
    public const string STATUS_ACCEPTED = "Accepted";
    public const string STATUS_CANCELLED = "Cancelled";
    public const string STATUS_EXPIRED = "Expired";

    /// <summary>
    /// Whether the invitation is still valid.
    /// </summary>
    public bool IsValid => Status == STATUS_PENDING && DateTime.UtcNow < ExpiresAt;

    public override int GetId() => UserInviteId;
    public override void SetId(int value) => UserInviteId = value;

    public override string ToString() => $"{Email} → {AccountNo} ({Status})";
}
