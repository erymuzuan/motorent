using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Integration methods for creating expenses from other modules.
/// </summary>
public partial class AssetExpenseService
{
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
}
