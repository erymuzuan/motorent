using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class AccidentService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    #region Accident CRUD

    public async Task<LoadOperation<Accident>> GetAccidentsAsync(
        int shopId,
        AccidentStatus? status = null,
        AccidentSeverity? severity = null,
        int? vehicleId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Accident>()
            .Where(a => a.ShopId == shopId)
            .OrderByDescending(a => a.AccidentDate);

        var result = await this.Context.LoadAsync(query, page, pageSize * 2, includeTotalRows: true);

        var filtered = result.ItemCollection.AsEnumerable();

        if (status.HasValue)
            filtered = filtered.Where(a => a.Status == status.Value);
        if (severity.HasValue)
            filtered = filtered.Where(a => a.Severity == severity.Value);
        if (vehicleId.HasValue)
            filtered = filtered.Where(a => a.VehicleId == vehicleId.Value);
        if (fromDate.HasValue)
            filtered = filtered.Where(a => a.AccidentDate >= fromDate.Value);
        if (toDate.HasValue)
            filtered = filtered.Where(a => a.AccidentDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            filtered = filtered.Where(a =>
                (a.Title?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.ReferenceNo?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.VehicleLicensePlate?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.RenterName?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.Description?.ToLowerInvariant().Contains(term) ?? false));
        }

        result.ItemCollection = filtered.Take(pageSize).ToList();
        return result;
    }

    public async Task<Accident?> GetAccidentByIdAsync(int accidentId)
    {
        return await this.Context.LoadOneAsync<Accident>(a => a.AccidentId == accidentId);
    }

    public async Task<string> GenerateReferenceNoAsync(DateTimeOffset accidentDate)
    {
        // Format: ACC-{YYYYMMDD}-{Sequence}
        var dateStr = accidentDate.ToString("yyyyMMdd");
        var prefix = $"ACC-{dateStr}-";

        var query = this.Context.CreateQuery<Accident>()
            .Where(a => a.ReferenceNo.StartsWith(prefix))
            .OrderByDescending(a => a.AccidentId);

        var result = await this.Context.LoadAsync(query, 1, 1, false);
        var lastAccident = result.ItemCollection.FirstOrDefault();

        var sequence = 1;
        if (lastAccident != null && !string.IsNullOrEmpty(lastAccident.ReferenceNo))
        {
            var parts = lastAccident.ReferenceNo.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var lastSeq))
                sequence = lastSeq + 1;
        }

        return $"{prefix}{sequence:D3}";
    }

    public async Task<SubmitOperation> CreateAccidentAsync(Accident accident, string username)
    {
        // Generate reference number if not set
        if (string.IsNullOrEmpty(accident.ReferenceNo))
            accident.ReferenceNo = await GenerateReferenceNoAsync(accident.AccidentDate);

        accident.ReportedDate = DateTimeOffset.Now;
        accident.Status = AccidentStatus.Reported;

        using var session = this.Context.OpenSession(username);
        session.Attach(accident);

        // Add initial note
        var note = new AccidentNote
        {
            AccidentId = accident.AccidentId,
            NoteType = "StatusChange",
            Content = "Accident reported.",
            NewStatus = AccidentStatus.Reported.ToString(),
            IsInternal = true
        };
        session.Attach(note);

        return await session.SubmitChanges("AccidentReported");
    }

    public async Task<SubmitOperation> UpdateAccidentAsync(Accident accident, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(accident);
        return await session.SubmitChanges("AccidentUpdated");
    }

    public async Task<SubmitOperation> UpdateStatusAsync(int accidentId, AccidentStatus newStatus, string? notes, string username)
    {
        var accident = await GetAccidentByIdAsync(accidentId);
        if (accident == null)
            return SubmitOperation.CreateFailure("Accident not found");

        var previousStatus = accident.Status;
        accident.Status = newStatus;

        if (newStatus == AccidentStatus.Resolved)
        {
            accident.ResolvedDate = DateTimeOffset.Now;
            accident.ResolvedBy = username;
            accident.ResolutionNotes = notes;
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(accident);

        // Add status change note
        var note = new AccidentNote
        {
            AccidentId = accidentId,
            NoteType = "StatusChange",
            Content = $"Status changed from {previousStatus} to {newStatus}. {notes ?? ""}".Trim(),
            PreviousStatus = previousStatus.ToString(),
            NewStatus = newStatus.ToString(),
            IsInternal = true
        };
        session.Attach(note);

        return await session.SubmitChanges("StatusChanged");
    }

    public async Task<SubmitOperation> DeleteAccidentAsync(Accident accident, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(accident);
        return await session.SubmitChanges("AccidentDeleted");
    }

    #endregion

    #region Parties

    public async Task<List<AccidentParty>> GetPartiesAsync(int accidentId)
    {
        var query = this.Context.CreateQuery<AccidentParty>()
            .Where(p => p.AccidentId == accidentId)
            .OrderBy(p => p.AccidentPartyId);

        var result = await this.Context.LoadAsync(query, 1, 100, false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> SavePartyAsync(AccidentParty party, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(party);
        return await session.SubmitChanges(party.AccidentPartyId == 0 ? "PartyAdded" : "PartyUpdated");
    }

    public async Task<SubmitOperation> DeletePartyAsync(AccidentParty party, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(party);
        return await session.SubmitChanges("PartyDeleted");
    }

    #endregion

    #region Documents

    public async Task<List<AccidentDocument>> GetDocumentsAsync(int accidentId)
    {
        var query = this.Context.CreateQuery<AccidentDocument>()
            .Where(d => d.AccidentId == accidentId)
            .OrderByDescending(d => d.UploadedDate);

        var result = await this.Context.LoadAsync(query, 1, 100, false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> SaveDocumentAsync(AccidentDocument document, string username)
    {
        if (document.AccidentDocumentId == 0)
        {
            document.UploadedDate = DateTimeOffset.Now;
            document.UploadedBy = username;
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(document);
        return await session.SubmitChanges(document.AccidentDocumentId == 0 ? "DocumentAdded" : "DocumentUpdated");
    }

    public async Task<SubmitOperation> DeleteDocumentAsync(AccidentDocument document, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(document);
        return await session.SubmitChanges("DocumentDeleted");
    }

    #endregion

    #region Costs

    public async Task<List<AccidentCost>> GetCostsAsync(int accidentId)
    {
        var query = this.Context.CreateQuery<AccidentCost>()
            .Where(c => c.AccidentId == accidentId)
            .OrderByDescending(c => c.AccidentCostId);

        var result = await this.Context.LoadAsync(query, 1, 100, false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> SaveCostAsync(AccidentCost cost, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(cost);
        var result = await session.SubmitChanges(cost.AccidentCostId == 0 ? "CostAdded" : "CostUpdated");

        // Recalculate financials after saving cost
        if (result.Success)
            await RecalculateFinancialsAsync(cost.AccidentId, username);

        return result;
    }

    public async Task<SubmitOperation> DeleteCostAsync(AccidentCost cost, string username)
    {
        var accidentId = cost.AccidentId;
        using var session = this.Context.OpenSession(username);
        session.Delete(cost);
        var result = await session.SubmitChanges("CostDeleted");

        // Recalculate financials after deleting cost
        if (result.Success)
            await RecalculateFinancialsAsync(accidentId, username);

        return result;
    }

    #endregion

    #region Notes

    public async Task<List<AccidentNote>> GetNotesAsync(int accidentId)
    {
        var query = this.Context.CreateQuery<AccidentNote>()
            .Where(n => n.AccidentId == accidentId)
            .OrderByDescending(n => n.AccidentNoteId);

        var result = await this.Context.LoadAsync(query, 1, 100, false);
        return result.ItemCollection;
    }

    public async Task<SubmitOperation> SaveNoteAsync(AccidentNote note, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(note);
        return await session.SubmitChanges(note.AccidentNoteId == 0 ? "NoteAdded" : "NoteUpdated");
    }

    public async Task<SubmitOperation> DeleteNoteAsync(AccidentNote note, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(note);
        return await session.SubmitChanges("NoteDeleted");
    }

    #endregion

    #region Financial Summary

    public async Task RecalculateFinancialsAsync(int accidentId, string username)
    {
        var accident = await GetAccidentByIdAsync(accidentId);
        if (accident == null) return;

        var costs = await GetCostsAsync(accidentId);

        accident.TotalEstimatedCost = costs.Where(c => !c.IsCredit).Sum(c => c.EstimatedAmount);
        accident.TotalActualCost = costs.Where(c => !c.IsCredit && c.ActualAmount.HasValue).Sum(c => c.ActualAmount!.Value);
        accident.InsurancePayoutReceived = costs.Where(c => c.IsCredit && c.ActualAmount.HasValue).Sum(c => c.ActualAmount!.Value);
        accident.NetCost = accident.TotalActualCost - accident.InsurancePayoutReceived;

        using var session = this.Context.OpenSession(username);
        session.Attach(accident);
        await session.SubmitChanges("FinancialsRecalculated");
    }

    public async Task<AccidentFinancialSummary> GetFinancialSummaryAsync(int accidentId)
    {
        var accident = await GetAccidentByIdAsync(accidentId);
        if (accident == null)
            return new AccidentFinancialSummary();

        var costs = await GetCostsAsync(accidentId);

        return new AccidentFinancialSummary
        {
            AccidentId = accidentId,
            TotalEstimatedCost = accident.TotalEstimatedCost,
            TotalActualCost = accident.TotalActualCost,
            ReserveAmount = accident.ReserveAmount,
            InsurancePayoutReceived = accident.InsurancePayoutReceived,
            NetCost = accident.NetCost,
            CostsByType = costs.GroupBy(c => c.CostType)
                .ToDictionary(
                    g => g.Key,
                    g => new CostTypeBreakdown
                    {
                        Estimated = g.Sum(c => c.EstimatedAmount),
                        Actual = g.Sum(c => c.ActualAmount ?? 0),
                        Pending = g.Where(c => !c.ActualAmount.HasValue).Sum(c => c.EstimatedAmount)
                    })
        };
    }

    #endregion

    #region Statistics

    public async Task<AccidentStatistics> GetStatisticsAsync(int shopId, DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
    {
        var query = this.Context.CreateQuery<Accident>()
            .Where(a => a.ShopId == shopId);

        var result = await this.Context.LoadAsync(query, 1, 1000, true);
        var accidents = result.ItemCollection.AsEnumerable();

        if (fromDate.HasValue)
            accidents = accidents.Where(a => a.AccidentDate >= fromDate.Value);
        if (toDate.HasValue)
            accidents = accidents.Where(a => a.AccidentDate <= toDate.Value);

        var list = accidents.ToList();

        return new AccidentStatistics
        {
            TotalAccidents = list.Count,
            ReportedCount = list.Count(a => a.Status == AccidentStatus.Reported),
            InProgressCount = list.Count(a => a.Status == AccidentStatus.InProgress),
            ResolvedCount = list.Count(a => a.Status == AccidentStatus.Resolved),
            TotalEstimatedCost = list.Sum(a => a.TotalEstimatedCost),
            TotalActualCost = list.Sum(a => a.TotalActualCost),
            TotalNetCost = list.Sum(a => a.NetCost),
            BySeverity = list.GroupBy(a => a.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            PoliceInvolvedCount = list.Count(a => a.PoliceInvolved),
            InsuranceClaimCount = list.Count(a => a.InsuranceClaimFiled)
        };
    }

    #endregion
}

public class AccidentFinancialSummary
{
    public int AccidentId { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal ReserveAmount { get; set; }
    public decimal InsurancePayoutReceived { get; set; }
    public decimal NetCost { get; set; }
    public Dictionary<AccidentCostType, CostTypeBreakdown> CostsByType { get; set; } = new();
}

public class CostTypeBreakdown
{
    public decimal Estimated { get; set; }
    public decimal Actual { get; set; }
    public decimal Pending { get; set; }
}

public class AccidentStatistics
{
    public int TotalAccidents { get; set; }
    public int ReportedCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal TotalNetCost { get; set; }
    public Dictionary<AccidentSeverity, int> BySeverity { get; set; } = new();
    public int PoliceInvolvedCount { get; set; }
    public int InsuranceClaimCount { get; set; }
}
