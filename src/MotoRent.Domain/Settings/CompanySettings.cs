namespace MotoRent.Domain.Settings;

/// <summary>
/// Company contact information configured in appsettings.json.
/// Bound via IOptions pattern for branding and contact pages.
/// </summary>
public class CompanySettings
{
    public const string SectionName = "CompanyInfo";

    public string Name { get; set; } = "MotoRent";
    public string? LegalName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
}
