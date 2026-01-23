using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing asset expenses.
/// </summary>
public class AssetExpenseService(RentalDataContext context, AssetService assetService)
{
    private RentalDataContext Context { get; } = context;
    private AssetService AssetService { get; } = assetService;

    #region Expense CRUD

    /// <summary>
    /// Get all expenses for an asset.
    /// </summary>
    public async Task<LoadOperation<AssetExpense>> GetExpensesAsync(
        int assetId,
        int page = 1,
        int size = 50)
    {
        var query = this.Context.CreateQuery<AssetExpense>()
            .Where(e => e.AssetId == assetId)
            .OrderByDescending(e => e.ExpenseDate);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Get expenses by category.
    /// </summary>
    public async Task<LoadOperation<AssetExpense>> GetExpensesByCategoryAsync(
        AssetExpenseCategory category,
        int page = 1,
        int size = 50)
    {
        var query = this.Context.CreateQuery<AssetExpense>()
            .Where(e => e.Category == category)
            .OrderByDescending(e => e.ExpenseDate);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Get expenses for a date range.
    /// </summary>
    public async Task<LoadOperation<AssetExpense>> GetExpensesForPeriodAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int page = 1,
        int size = 100)
    {
        var query = this.Context.CreateQuery<AssetExpense>()
            .Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate)
            .OrderByDescending(e => e.ExpenseDate);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Get expense by ID.
    /// </summary>
    public async Task<AssetExpense?> GetExpenseByIdAsync(int expenseId)
    {
        return await this.Context.LoadOneAsync<AssetExpense>(e => e.AssetExpenseId == expenseId);
    }

    /// <summary>
    /// Create a new expense.
    /// </summary>
    public async Task<SubmitOperation> CreateExpenseAsync(AssetExpense expense, string username)
    {
        // Set accounting period if not set
        if (string.IsNullOrEmpty(expense.AccountingPeriod))
            expense.AccountingPeriod = expense.ExpenseDate.ToString("yyyy-MM");

        using var session = this.Context.OpenSession(username);
        session.Attach(expense);
        var result = await session.SubmitChanges("CreateExpense");

        // Update asset total if successful
        if (result.Success)
        {
            await this.AssetService.UpdateExpenseTotalAsync(expense.AssetId, expense.Amount, username);
        }

        return result;
    }

    /// <summary>
    /// Update an existing expense.
    /// </summary>
    public async Task<SubmitOperation> UpdateExpenseAsync(
        AssetExpense expense,
        decimal previousAmount,
        string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(expense);
        var result = await session.SubmitChanges("UpdateExpense");

        // Update asset total with difference
        if (result.Success)
        {
            var difference = expense.Amount - previousAmount;
            if (difference != 0)
            {
                await this.AssetService.UpdateExpenseTotalAsync(expense.AssetId, difference, username);
            }
        }

        return result;
    }

    /// <summary>
    /// Delete an expense.
    /// </summary>
    public async Task<SubmitOperation> DeleteExpenseAsync(AssetExpense expense, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(expense);
        var result = await session.SubmitChanges("DeleteExpense");

        // Update asset total (subtract the deleted amount)
        if (result.Success)
        {
            await this.AssetService.UpdateExpenseTotalAsync(expense.AssetId, -expense.Amount, username);
        }

        return result;
    }

    #endregion

    #region Integration Methods

    /// <summary>
    /// Create expense from a maintenance service record.
    /// </summary>
    public async Task<SubmitOperation> CreateFromMaintenanceAsync(
        int assetId,
        int maintenanceScheduleId,
        decimal cost,
        DateTimeOffset serviceDate,
        string description,
        string username)
    {
        var expense = new AssetExpense
        {
            AssetId = assetId,
            Category = AssetExpenseCategory.Maintenance,
            Amount = cost,
            ExpenseDate = serviceDate,
            MaintenanceScheduleId = maintenanceScheduleId,
            Description = description,
            IsPaid = true,
            IsTaxDeductible = true
        };

        return await this.CreateExpenseAsync(expense, username);
    }

    /// <summary>
    /// Create expense from an accident cost.
    /// </summary>
    public async Task<SubmitOperation> CreateFromAccidentAsync(
        int assetId,
        int accidentId,
        decimal cost,
        DateTimeOffset accidentDate,
        string description,
        string username)
    {
        var expense = new AssetExpense
        {
            AssetId = assetId,
            Category = AssetExpenseCategory.Accident,
            Amount = cost,
            ExpenseDate = accidentDate,
            AccidentId = accidentId,
            Description = description,
            IsPaid = false, // Accident costs may need to be paid later
            IsTaxDeductible = true
        };

        return await this.CreateExpenseAsync(expense, username);
    }

    /// <summary>
    /// Create expense from a loan payment interest portion.
    /// </summary>
    public async Task<SubmitOperation> CreateFromLoanPaymentAsync(
        int assetId,
        int loanPaymentId,
        decimal interestAmount,
        DateTimeOffset paymentDate,
        string description,
        string username)
    {
        var expense = new AssetExpense
        {
            AssetId = assetId,
            Category = AssetExpenseCategory.Financing,
            Amount = interestAmount,
            ExpenseDate = paymentDate,
            AssetLoanPaymentId = loanPaymentId,
            Description = description,
            IsPaid = true,
            IsTaxDeductible = true
        };

        return await this.CreateExpenseAsync(expense, username);
    }

    /// <summary>
    /// Create expense from a rental (fuel/consumables).
    /// </summary>
    public async Task<SubmitOperation> CreateFromRentalAsync(
        int assetId,
        int rentalId,
        AssetExpenseCategory category,
        decimal amount,
        DateTimeOffset date,
        string description,
        string username)
    {
        var expense = new AssetExpense
        {
            AssetId = assetId,
            Category = category,
            Amount = amount,
            ExpenseDate = date,
            RentalId = rentalId,
            Description = description,
            IsPaid = true,
            IsTaxDeductible = true
        };

        return await this.CreateExpenseAsync(expense, username);
    }

    #endregion

    #region Reporting

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

    #endregion
}

#region DTOs

/// <summary>
/// Asset expense summary.
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
/// Monthly expense trend.
/// </summary>
public class MonthlyExpenseTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public Dictionary<AssetExpenseCategory, decimal> ByCategory { get; set; } = new();
}

#endregion
