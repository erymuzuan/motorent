using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Tourist;
using MotoRent.Services.Tourist;

namespace MotoRent.Services;

/// <summary>
/// Service for managing Organization entities and branding settings.
/// </summary>
public class OrganizationService(CoreDataContext context, ITenantResolverService? tenantResolver = null)
{
    private CoreDataContext Context { get; } = context;
    private ITenantResolverService? TenantResolver { get; } = tenantResolver;

    /// <summary>
    /// Gets all active organizations with pagination.
    /// </summary>
    public async Task<LoadOperation<Organization>> GetOrganizationsAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.Organizations
            .Where(o => o.IsActive);

        query = query.OrderBy(o => o.Name);

        var result = await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(o =>
                    (o.Name?.ToLowerInvariant().Contains(term) ?? false) ||
                    (o.AccountNo?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    /// <summary>
    /// Gets an organization by its ID.
    /// </summary>
    public async Task<Organization?> GetOrganizationByIdAsync(int organizationId)
    {
        return await this.Context.LoadOneAsync<Organization>(o => o.OrganizationId == organizationId);
    }

    /// <summary>
    /// Gets an organization by its AccountNo.
    /// </summary>
    public async Task<Organization?> GetOrganizationByAccountNoAsync(string accountNo)
    {
        return await this.Context.LoadOneAsync<Organization>(o => o.AccountNo == accountNo);
    }

    /// <summary>
    /// Gets an organization by its custom domain.
    /// </summary>
    public async Task<Organization?> GetOrganizationByCustomDomainAsync(string domain)
    {
        var result = await this.Context.LoadAsync(
            this.Context.Organizations.Where(o => o.IsActive),
            page: 1, size: 1000, includeTotalRows: false);

        return result.ItemCollection.FirstOrDefault(o =>
            string.Equals(o.CustomDomain, domain, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Updates an organization's general settings.
    /// </summary>
    public async Task<SubmitOperation> UpdateOrganizationAsync(Organization organization, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(organization);
        return await session.SubmitChanges("UpdateOrganization");
    }

    /// <summary>
    /// Updates an organization's branding settings.
    /// </summary>
    public async Task<SubmitOperation> UpdateBrandingAsync(
        string accountNo,
        TenantBranding branding,
        string username)
    {
        var org = await this.GetOrganizationByAccountNoAsync(accountNo);
        if (org == null)
            return SubmitOperation.CreateFailure("Organization not found");

        org.Branding = branding;

        using var session = this.Context.OpenSession(username);
        session.Attach(org);
        var result = await session.SubmitChanges("UpdateBranding");

        // Invalidate tenant cache on successful update
        if (result.Success)
            this.TenantResolver?.InvalidateCache(accountNo);

        return result;
    }

    /// <summary>
    /// Updates an organization's custom domain.
    /// </summary>
    public async Task<SubmitOperation> UpdateCustomDomainAsync(
        string accountNo,
        string? customDomain,
        string username)
    {
        var org = await this.GetOrganizationByAccountNoAsync(accountNo);
        if (org == null)
            return SubmitOperation.CreateFailure("Organization not found");

        // Validate custom domain is unique if set
        if (!string.IsNullOrWhiteSpace(customDomain))
        {
            var existingOrg = await this.GetOrganizationByCustomDomainAsync(customDomain);
            if (existingOrg != null && existingOrg.AccountNo != accountNo)
                return SubmitOperation.CreateFailure($"Custom domain '{customDomain}' is already in use");
        }

        org.CustomDomain = string.IsNullOrWhiteSpace(customDomain) ? null : customDomain.Trim().ToLowerInvariant();

        using var session = this.Context.OpenSession(username);
        session.Attach(org);
        var result = await session.SubmitChanges("UpdateCustomDomain");

        // Invalidate tenant cache on successful update
        if (result.Success)
            this.TenantResolver?.InvalidateCache(accountNo);

        return result;
    }

    /// <summary>
    /// Creates a new organization.
    /// </summary>
    public async Task<SubmitOperation> CreateOrganizationAsync(Organization organization, string username)
    {
        // Validate AccountNo is unique
        var existing = await this.GetOrganizationByAccountNoAsync(organization.AccountNo);
        if (existing != null)
            return SubmitOperation.CreateFailure($"AccountNo '{organization.AccountNo}' is already in use");

        using var session = this.Context.OpenSession(username);
        session.Attach(organization);
        return await session.SubmitChanges("CreateOrganization");
    }

    /// <summary>
    /// Deactivates an organization (soft delete).
    /// </summary>
    public async Task<SubmitOperation> DeactivateOrganizationAsync(string accountNo, string username)
    {
        var org = await this.GetOrganizationByAccountNoAsync(accountNo);
        if (org == null)
            return SubmitOperation.CreateFailure("Organization not found");

        org.IsActive = false;

        using var session = this.Context.OpenSession(username);
        session.Attach(org);
        return await session.SubmitChanges("Deactivate");
    }
}
