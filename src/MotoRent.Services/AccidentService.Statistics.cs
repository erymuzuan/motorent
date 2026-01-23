using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Accident statistics and reporting operations.
/// </summary>
public partial class AccidentService
{
    public async Task<AccidentStatistics> GetStatisticsAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
    {
        var query = this.Context.CreateQuery<Accident>();

        // Apply date filters at SQL level
        if (fromDate.HasValue)
            query = query.Where(a => a.AccidentDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(a => a.AccidentDate <= toDate.Value);

        // Get counts using SQL
        var totalCount = await this.Context.GetCountAsync(query);
        var reportedCount = await this.Context.GetCountAsync(query.Where(a => a.Status == AccidentStatus.Reported));
        var inProgressCount = await this.Context.GetCountAsync(query.Where(a => a.Status == AccidentStatus.InProgress));
        var resolvedCount = await this.Context.GetCountAsync(query.Where(a => a.Status == AccidentStatus.Resolved));

        // Get sums using SQL
        var totalEstimatedCost = await this.Context.GetSumAsync(query, a => a.TotalEstimatedCost);
        var totalActualCost = await this.Context.GetSumAsync(query, a => a.TotalActualCost);
        var totalNetCost = await this.Context.GetSumAsync(query, a => a.NetCost);

        // Get severity breakdown using SQL GROUP BY
        var severityCounts = await this.Context.GetGroupByCountAsync(query, a => a.Severity);

        // Get boolean counts using SQL COUNT
        var policeInvolvedCount = await this.Context.GetCountAsync(query.Where(a => a.PoliceInvolved));
        var insuranceClaimCount = await this.Context.GetCountAsync(query.Where(a => a.InsuranceClaimFiled));

        return new AccidentStatistics
        {
            TotalAccidents = totalCount,
            ReportedCount = reportedCount,
            InProgressCount = inProgressCount,
            ResolvedCount = resolvedCount,
            TotalEstimatedCost = totalEstimatedCost,
            TotalActualCost = totalActualCost,
            TotalNetCost = totalNetCost,
            BySeverity = severityCounts.ToDictionary(g => g.Key, g => g.Count),
            PoliceInvolvedCount = policeInvolvedCount,
            InsuranceClaimCount = insuranceClaimCount
        };
    }
}

/// <summary>
/// Accident statistics DTO.
/// </summary>
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
