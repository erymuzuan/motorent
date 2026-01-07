using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Invitation code for controlled tenant onboarding.
/// Allows limiting the number of registrations and validity period.
/// </summary>
public class RegistrationInvite : Entity
{
    public int RegistrationInviteId { get; set; }

    /// <summary>
    /// Unique invitation code.
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Optional note/description for this invite.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Date from which the invite is valid.
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Date until which the invite is valid.
    /// </summary>
    public DateTime ValidTo { get; set; }

    /// <summary>
    /// Maximum number of accounts that can register with this code.
    /// </summary>
    public int MaxAccount { get; set; }

    /// <summary>
    /// List of AccountNos that have registered using this code.
    /// </summary>
    public List<string> RegisteredAccounts { get; } = [];

    /// <summary>
    /// Whether the invite is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional free credit amount for new registrations.
    /// </summary>
    public decimal? FreeCredit { get; set; }

    /// <summary>
    /// Number of days the free credit is valid.
    /// </summary>
    public int? CreditValidityDays { get; set; }

    /// <summary>
    /// Number of remaining registration slots.
    /// </summary>
    public int Remainder => MaxAccount - RegisteredAccounts.Count;

    /// <summary>
    /// Whether the invite is currently valid.
    /// </summary>
    public bool Valid
    {
        get
        {
            if (!IsActive) return false;
            if (DateTime.Today < ValidFrom) return false;
            if (DateTime.Today > ValidTo) return false;
            if (Remainder <= 0) return false;
            return true;
        }
    }

    /// <summary>
    /// Registers an account using this invite.
    /// </summary>
    public bool TryRegister(string accountNo)
    {
        if (!Valid) return false;
        if (RegisteredAccounts.Contains(accountNo)) return false;
        RegisteredAccounts.Add(accountNo);
        return true;
    }

    public override int GetId() => RegistrationInviteId;
    public override void SetId(int value) => RegistrationInviteId = value;

    public override string ToString() => $"{Code} ({Remainder}/{MaxAccount})";
}
