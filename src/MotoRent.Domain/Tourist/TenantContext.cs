namespace MotoRent.Domain.Tourist;

/// <summary>
/// Represents the resolved tenant context for tourist-facing pages.
/// Cascaded through TouristLayout to all tourist components.
/// </summary>
public class TenantContext
{
    /// <summary>
    /// The unique tenant identifier.
    /// </summary>
    public string AccountNo { get; set; } = "";

    /// <summary>
    /// The organization's display name.
    /// </summary>
    public string OrganizationName { get; set; } = "";

    /// <summary>
    /// URL to the organization's main logo.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// URL to the organization's small/icon logo.
    /// </summary>
    public string? SmallLogoUrl { get; set; }

    /// <summary>
    /// Currency code for price display (default: THB).
    /// </summary>
    public string Currency { get; set; } = "THB";

    /// <summary>
    /// Timezone offset from UTC (default: 7 for Thailand).
    /// </summary>
    public double Timezone { get; set; } = 7;

    /// <summary>
    /// Language/locale code for UI (default: th-TH).
    /// </summary>
    public string Language { get; set; } = "th-TH";

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Organization website URL.
    /// </summary>
    public string? WebSite { get; set; }

    /// <summary>
    /// Branding configuration for tourist-facing pages.
    /// </summary>
    public TenantBranding Branding { get; set; } = new();

    /// <summary>
    /// Indicates whether the tenant context has been successfully loaded.
    /// </summary>
    public bool IsLoaded => !string.IsNullOrEmpty(AccountNo);
}

/// <summary>
/// Branding configuration for customizing tourist-facing pages.
/// </summary>
public class TenantBranding
{
    /// <summary>
    /// Primary theme color (default: Tropical Teal #00897B).
    /// </summary>
    public string PrimaryColor { get; set; } = "#00897B";

    /// <summary>
    /// Secondary theme color (default: Dark Teal #004D40).
    /// </summary>
    public string SecondaryColor { get; set; } = "#004D40";

    /// <summary>
    /// Accent color for highlights and CTAs (default: Light Teal #26A69A).
    /// </summary>
    public string AccentColor { get; set; } = "#26A69A";

    /// <summary>
    /// Text color for content on primary background (default: White).
    /// </summary>
    public string TextOnPrimary { get; set; } = "#FFFFFF";

    /// <summary>
    /// Layout template name: "Modern" (default), "Classic", or "Minimal".
    /// </summary>
    public string LayoutTemplate { get; set; } = "Modern";

    /// <summary>
    /// Custom CSS to inject into the tourist pages.
    /// Only editable by SuperAdmin for security.
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// URL or store ID for the hero image on landing/browse pages.
    /// </summary>
    public string? HeroImageUrl { get; set; }

    /// <summary>
    /// Custom footer text (HTML allowed but sanitized).
    /// </summary>
    public string? FooterText { get; set; }

    /// <summary>
    /// Social media links for footer display.
    /// Format: ["facebook:url", "instagram:url", "line:id"]
    /// </summary>
    public string[]? SocialLinks { get; set; }

    /// <summary>
    /// Whether to show contact info in header/footer.
    /// </summary>
    public bool ShowContactInfo { get; set; } = true;

    /// <summary>
    /// Custom tagline displayed under organization name.
    /// </summary>
    public string? Tagline { get; set; }
}
