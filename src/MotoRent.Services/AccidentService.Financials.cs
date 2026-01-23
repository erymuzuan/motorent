using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Accident financial summary operations.
/// </summary>
public partial class AccidentService
{
    public async Task RecalculateFinancialsAsync(int accidentId, string username)
    {
        var accident = await this.GetAccidentByIdAsync(accidentId);
        if (accident == null) return;

        var costs = await this.GetCostsAsync(accidentId);

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
        var accident = await this.GetAccidentByIdAsync(accidentId);
        if (accident == null)
            return new AccidentFinancialSummary();

        var costs = await this.GetCostsAsync(accidentId);

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
}

/// <summary>
/// Accident financial summary DTO.
/// </summary>
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

/// <summary>
/// Cost breakdown by type.
/// </summary>
public class CostTypeBreakdown
{
    public decimal Estimated { get; set; }
    public decimal Actual { get; set; }
    public decimal Pending { get; set; }
}
