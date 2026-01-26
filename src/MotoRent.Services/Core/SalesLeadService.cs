using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Core;

/// <summary>
/// Service for managing sales leads - CRUD, status transitions, and statistics.
/// </summary>
public class SalesLeadService
{
    private readonly CoreDataContext m_context;

    public SalesLeadService(CoreDataContext context)
    {
        m_context = context;
    }

    #region Create

    /// <summary>
    /// Creates a new lead from a contact form submission.
    /// </summary>
    public async Task<SubmitOperation> CreateLeadAsync(SalesLead lead, string username = "system")
    {
        // Set defaults
        lead.Status = LeadStatus.Lead;
        if (lead.Source == default)
            lead.Source = LeadSource.ContactForm;

        // Insert first to get the ID
        using var session = m_context.OpenSession(username);
        session.Attach(lead);
        var result = await session.SubmitChanges("CreateLead");

        if (!result.Success)
            return result;

        // Generate serial number based on ID (SL-00001 format)
        lead.No = $"SL-{lead.SalesLeadId:D5}";

        // Update with serial number
        using var updateSession = m_context.OpenSession(username);
        updateSession.Attach(lead);
        return await updateSession.SubmitChanges("SetSerialNumber");
    }

    #endregion

    #region Read

    /// <summary>
    /// Gets leads with filtering and pagination.
    /// </summary>
    public async Task<LoadOperation<SalesLead>> GetLeadsAsync(
        LeadStatus? status = null,
        LeadSource? source = null,
        string? search = null,
        int page = 1,
        int size = 20)
    {
        var query = m_context.SalesLeads.AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (source.HasValue)
            query = query.Where(x => x.Source == source.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(x =>
                (x.Name != null && x.Name.Contains(search)) ||
                (x.Email != null && x.Email.Contains(search)) ||
                (x.Company != null && x.Company.Contains(search)));

        query = query.OrderByDescending(x => x.SalesLeadId);

        return await m_context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Gets a single lead by ID.
    /// </summary>
    public async Task<SalesLead?> GetByIdAsync(int id)
    {
        return await m_context.LoadOneAsync<SalesLead>(x => x.SalesLeadId == id);
    }

    /// <summary>
    /// Gets a lead by email address.
    /// </summary>
    public async Task<SalesLead?> GetByEmailAsync(string email)
    {
        return await m_context.LoadOneAsync<SalesLead>(x => x.Email == email);
    }

    /// <summary>
    /// Checks if a lead with the given email already exists.
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await m_context.ExistAsync<SalesLead>(x => x.Email == email);
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing lead.
    /// </summary>
    public async Task<SubmitOperation> UpdateLeadAsync(SalesLead lead, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(lead);
        return await session.SubmitChanges("UpdateLead");
    }

    /// <summary>
    /// Adds a note to a lead.
    /// </summary>
    public async Task<SubmitOperation> AddNoteAsync(int id, string note, string username)
    {
        var lead = await GetByIdAsync(id);
        if (lead is null)
            return SubmitOperation.CreateFailure("Lead not found");

        lead.Notes = string.IsNullOrEmpty(lead.Notes)
            ? $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {username}: {note}"
            : $"{lead.Notes}\n[{DateTime.Now:yyyy-MM-dd HH:mm}] {username}: {note}";

        using var session = m_context.OpenSession(username);
        session.Attach(lead);
        return await session.SubmitChanges("AddNote");
    }

    #endregion

    #region Status Transitions

    /// <summary>
    /// Marks a lead as starting trial.
    /// </summary>
    public async Task<SubmitOperation> MarkAsTrialAsync(int id, string username)
    {
        var lead = await GetByIdAsync(id);
        if (lead is null)
            return SubmitOperation.CreateFailure("Lead not found");

        if (lead.Status != LeadStatus.Lead)
            return SubmitOperation.CreateFailure("Can only start trial from Lead status");

        lead.Status = LeadStatus.Trial;
        lead.TrialStartedAt = DateTimeOffset.Now;

        using var session = m_context.OpenSession(username);
        session.Attach(lead);
        return await session.SubmitChanges("MarkAsTrial");
    }

    /// <summary>
    /// Marks a lead as converted to customer.
    /// </summary>
    public async Task<SubmitOperation> MarkAsCustomerAsync(int id, int organizationId, string accountNo, string username)
    {
        var lead = await GetByIdAsync(id);
        if (lead is null)
            return SubmitOperation.CreateFailure("Lead not found");

        lead.Status = LeadStatus.Customer;
        lead.ConvertedAt = DateTimeOffset.Now;
        lead.OrganizationId = organizationId;
        lead.AccountNo = accountNo;

        using var session = m_context.OpenSession(username);
        session.Attach(lead);
        return await session.SubmitChanges("MarkAsCustomer");
    }

    #endregion

    #region Delete

    /// <summary>
    /// Deletes a lead.
    /// </summary>
    public async Task<SubmitOperation> DeleteAsync(int id, string username)
    {
        var lead = await GetByIdAsync(id);
        if (lead is null)
            return SubmitOperation.CreateFailure("Lead not found");

        using var session = m_context.OpenSession(username);
        session.Delete(lead);
        return await session.SubmitChanges("DeleteLead");
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets lead statistics for dashboard display.
    /// </summary>
    public async Task<LeadStats> GetStatsAsync()
    {
        var allLo = await m_context.LoadAsync(m_context.SalesLeads, 1, 1, includeTotalRows: true);

        var leadQuery = m_context.SalesLeads.Where(x => x.Status == LeadStatus.Lead);
        var leadLo = await m_context.LoadAsync(leadQuery, 1, 1, includeTotalRows: true);

        var trialQuery = m_context.SalesLeads.Where(x => x.Status == LeadStatus.Trial);
        var trialLo = await m_context.LoadAsync(trialQuery, 1, 1, includeTotalRows: true);

        var customerQuery = m_context.SalesLeads.Where(x => x.Status == LeadStatus.Customer);
        var customerLo = await m_context.LoadAsync(customerQuery, 1, 1, includeTotalRows: true);

        return new LeadStats(
            allLo.TotalRows,
            leadLo.TotalRows,
            trialLo.TotalRows,
            customerLo.TotalRows
        );
    }

    #endregion
}

#region DTOs

/// <summary>
/// Statistics for lead dashboard.
/// </summary>
public record LeadStats(
    int TotalCount,
    int LeadCount,
    int TrialCount,
    int CustomerCount
)
{
    /// <summary>
    /// Conversion rate from lead to customer.
    /// </summary>
    public decimal ConversionRate => TotalCount > 0
        ? Math.Round((decimal)CustomerCount / TotalCount * 100, 1)
        : 0;

    /// <summary>
    /// Trial rate from lead to trial.
    /// </summary>
    public decimal TrialRate => TotalCount > 0
        ? Math.Round((decimal)(TrialCount + CustomerCount) / TotalCount * 100, 1)
        : 0;
}

#endregion
