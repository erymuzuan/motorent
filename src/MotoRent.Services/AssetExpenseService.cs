using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing asset expenses.
/// </summary>
public partial class AssetExpenseService(RentalDataContext context, AssetService assetService)
{
    private RentalDataContext Context { get; } = context;
    private AssetService AssetService { get; } = assetService;

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
}
