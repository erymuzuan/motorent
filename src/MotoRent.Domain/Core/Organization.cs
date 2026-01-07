using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Represents a tenant (organization) in the multi-tenant system.
/// Each organization can have multiple shops and users.
/// </summary>
public class Organization : Entity
{
    public int OrganizationId { get; set; }

    /// <summary>
    /// Unique tenant identifier used for data isolation.
    /// </summary>
    public string AccountNo { get; set; } = "";

    /// <summary>
    /// Organization display name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Currency code (default: THB for Thailand).
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Timezone offset from UTC (default: 7 for Thailand).
    /// </summary>
    public double? Timezone { get; set; } = 7;

    /// <summary>
    /// Language/locale code (default: th-TH for Thailand).
    /// </summary>
    public string Language { get; set; } = "th-TH";

    /// <summary>
    /// First day of the week for calendar displays.
    /// </summary>
    public DayOfWeek? FirstDay { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Feature subscriptions enabled for this organization.
    /// </summary>
    public string[] Subscriptions { get; set; } = [];

    /// <summary>
    /// Whether the organization is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Organization website URL.
    /// </summary>
    public string? WebSite { get; set; }

    /// <summary>
    /// Default start page for users in this organization.
    /// </summary>
    public string? DefaultStartPage { get; set; }

    /// <summary>
    /// Company registration number.
    /// </summary>
    public string? CompanyNo { get; set; }

    /// <summary>
    /// Tax registration number.
    /// </summary>
    public string? TaxNo { get; set; }

    /// <summary>
    /// Logo store ID for main logo.
    /// </summary>
    public string? LogoStoreId { get; set; }

    /// <summary>
    /// Logo store ID for small/icon logo.
    /// </summary>
    public string? SmallLogoStoreId { get; set; }

    /// <summary>
    /// Contact email for the organization.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone for the organization.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Organization address.
    /// </summary>
    public Address Address { get; set; } = new();

    public override int GetId() => OrganizationId;
    public override void SetId(int value) => OrganizationId = value;

    public override string ToString() => $"{Name} ({AccountNo})";
}

/// <summary>
/// Address information for an organization.
/// </summary>
public class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "Thailand";
}
