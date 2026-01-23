using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing accidents and related entities.
/// </summary>
public partial class AccidentService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<LoadOperation<Accident>> GetAccidentsAsync(
        AccidentStatus? status = null,
        AccidentSeverity? severity = null,
        int? vehicleId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<Accident>();

        // Apply filters at SQL level
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);
        if (vehicleId.HasValue)
            query = query.Where(a => a.VehicleId == vehicleId.Value);
        if (fromDate.HasValue)
            query = query.Where(a => a.AccidentDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(a => a.AccidentDate <= toDate.Value);

        query = query.OrderByDescending(a => a.AccidentDate);

        // If no search term, return SQL-filtered results
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
        }

        // For search term, load more and filter in memory
        var result = await this.Context.LoadAsync(query, page, pageSize * 2, includeTotalRows: true);

        var term = searchTerm.ToLowerInvariant();
        result.ItemCollection = result.ItemCollection
            .Where(a =>
                (a.Title?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.ReferenceNo?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.VehicleLicensePlate?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.RenterName?.ToLowerInvariant().Contains(term) ?? false) ||
                (a.Description?.ToLowerInvariant().Contains(term) ?? false))
            .Take(pageSize)
            .ToList();

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
            accident.ReferenceNo = await this.GenerateReferenceNoAsync(accident.AccidentDate);

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
        var accident = await this.GetAccidentByIdAsync(accidentId);
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
}
