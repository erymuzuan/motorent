using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Accident cost management operations.
/// </summary>
public partial class AccidentService
{
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
            await this.RecalculateFinancialsAsync(cost.AccidentId, username);

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
            await this.RecalculateFinancialsAsync(accidentId, username);

        return result;
    }
}
