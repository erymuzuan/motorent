using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Reporting and analytics methods for asset expenses.
/// </summary>
public partial class AssetExpenseService
{
    /// <summary>
    /// Get expenses grouped by category for all assets.
    /// </summary>
    public async Task<Dictionary<AssetExpenseCategory, decimal>> GetExpensesByCategoryAsync()
    {
        // Use SQL GROUP BY SUM
        var query = this.Context.CreateQuery<AssetExpense>();
        var groupSums = await this.Context.GetGroupBySumAsync(query, e => e.Category, e => e.Amount);

        return groupSums.ToDictionary(g => g.Key, g => g.Sum);
    }

    /// <summary>
    /// Get expense summary for an asset.
    /// </summary>
    public async Task<AssetExpenseSummary> GetExpenseSummaryAsync(int assetId)
    {
        var expensesResult = await this.GetExpensesAsync(assetId, 1, 10000);
        var expenses = expensesResult.ItemCollection;

        return new AssetExpenseSummary
        {
            AssetId = assetId,
            TotalExpenses = expenses.Sum(e => e.Amount),
            ByCategory = expenses
                .GroupBy(e => e.Category)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)),
            TaxDeductible = expenses.Where(e => e.IsTaxDeductible).Sum(e => e.Amount),
            UnpaidExpenses = expenses.Where(e => !e.IsPaid).Sum(e => e.Amount),
            ExpenseCount = expenses.Count
        };
    }

    /// <summary>
    /// Get monthly expense trend for an asset.
    /// </summary>
    public async Task<List<MonthlyExpenseTrend>> GetMonthlyTrendAsync(int assetId, int monthsBack = 12)
    {
        var startDate = DateTimeOffset.Now.AddMonths(-monthsBack);
        var expensesResult = await this.GetExpensesAsync(assetId, 1, 10000);

        var grouped = expensesResult.ItemCollection
            .Where(e => e.ExpenseDate >= startDate)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new MonthlyExpenseTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                TotalAmount = g.Sum(e => e.Amount),
                ByCategory = g.GroupBy(e => e.Category)
                    .ToDictionary(cg => cg.Key, cg => cg.Sum(e => e.Amount))
            })
            .OrderBy(t => t.Year)
            .ThenBy(t => t.Month)
            .ToList();

        return grouped;
    }

    /// <summary>
    /// Get unpaid expenses.
    /// </summary>
    public async Task<LoadOperation<AssetExpense>> GetUnpaidExpensesAsync(int page = 1, int size = 50)
    {
        var query = this.Context.CreateQuery<AssetExpense>()
            .Where(e => !e.IsPaid)
            .OrderByDescending(e => e.ExpenseDate);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Mark expense as paid.
    /// </summary>
    public async Task<SubmitOperation> MarkAsPaidAsync(int expenseId, string username)
    {
        var expense = await this.GetExpenseByIdAsync(expenseId);
        if (expense == null)
            return SubmitOperation.CreateFailure("Expense not found");

        expense.IsPaid = true;

        using var session = this.Context.OpenSession(username);
        session.Attach(expense);
        return await session.SubmitChanges("MarkExpensePaid");
    }
}

/// <summary>
/// Asset expense summary DTO.
/// </summary>
public class AssetExpenseSummary
{
    public int AssetId { get; set; }
    public decimal TotalExpenses { get; set; }
    public Dictionary<AssetExpenseCategory, decimal> ByCategory { get; set; } = new();
    public decimal TaxDeductible { get; set; }
    public decimal UnpaidExpenses { get; set; }
    public int ExpenseCount { get; set; }
}

/// <summary>
/// Monthly expense trend DTO.
/// </summary>
public class MonthlyExpenseTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public Dictionary<AssetExpenseCategory, decimal> ByCategory { get; set; } = new();
}
