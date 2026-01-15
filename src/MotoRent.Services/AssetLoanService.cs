using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing asset loans and amortization.
/// </summary>
public class AssetLoanService(RentalDataContext context, AssetExpenseService expenseService)
{
    private RentalDataContext Context { get; } = context;
    private AssetExpenseService ExpenseService { get; } = expenseService;

    #region Loan CRUD

    /// <summary>
    /// Get all active loans.
    /// </summary>
    public async Task<LoadOperation<AssetLoan>> GetLoansAsync(
        LoanStatus? status = null,
        int page = 1,
        int size = 50)
    {
        var query = this.Context.CreateQuery<AssetLoan>();

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        query = query.OrderByDescending(l => l.StartDate);

        return await this.Context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Get loan by ID.
    /// </summary>
    public async Task<AssetLoan?> GetLoanByIdAsync(int loanId)
    {
        return await this.Context.LoadOneAsync<AssetLoan>(l => l.AssetLoanId == loanId);
    }

    /// <summary>
    /// Get loan for an asset.
    /// </summary>
    public async Task<AssetLoan?> GetLoanByAssetIdAsync(int assetId)
    {
        return await this.Context.LoadOneAsync<AssetLoan>(l => l.AssetId == assetId);
    }

    /// <summary>
    /// Create a new loan.
    /// </summary>
    public async Task<SubmitOperation> CreateLoanAsync(AssetLoan loan, string username)
    {
        // Calculate monthly payment if not set
        if (loan.MonthlyPayment == 0)
        {
            loan.MonthlyPayment = this.CalculateMonthlyPayment(
                loan.PrincipalAmount,
                loan.AnnualInterestRate,
                loan.TermMonths);
        }

        // Set initial values
        loan.RemainingPrincipal = loan.PrincipalAmount;
        loan.TotalPrincipalPaid = 0;
        loan.TotalInterestPaid = 0;
        loan.PaymentsMade = 0;
        loan.Status = LoanStatus.Active;
        loan.NextPaymentDue = loan.StartDate.AddMonths(1);
        loan.EndDate = loan.StartDate.AddMonths(loan.TermMonths);

        using var session = this.Context.OpenSession(username);
        session.Attach(loan);
        var result = await session.SubmitChanges("CreateLoan");

        // Generate amortization schedule
        if (result.Success)
        {
            await this.GeneratePaymentScheduleAsync(loan.AssetLoanId, username);
        }

        return result;
    }

    /// <summary>
    /// Update an existing loan.
    /// </summary>
    public async Task<SubmitOperation> UpdateLoanAsync(AssetLoan loan, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(loan);
        return await session.SubmitChanges("UpdateLoan");
    }

    #endregion

    #region Payment Calculations

    /// <summary>
    /// Calculate monthly payment using PMT formula.
    /// </summary>
    public decimal CalculateMonthlyPayment(
        decimal principal,
        decimal annualRate,
        int termMonths)
    {
        if (termMonths <= 0)
            return 0;

        // For 0% interest, just divide principal by term
        if (annualRate == 0)
            return Math.Round(principal / termMonths, 2);

        var monthlyRate = annualRate / 12;
        var factor = (double)monthlyRate * Math.Pow(1 + (double)monthlyRate, termMonths);
        var divisor = Math.Pow(1 + (double)monthlyRate, termMonths) - 1;

        var payment = (double)principal * (factor / divisor);
        return Math.Round((decimal)payment, 2);
    }

    /// <summary>
    /// Generate full amortization schedule.
    /// </summary>
    public List<AmortizationEntry> GenerateAmortizationSchedule(
        decimal principal,
        decimal annualRate,
        int termMonths,
        DateTimeOffset startDate)
    {
        var schedule = new List<AmortizationEntry>();
        var monthlyPayment = this.CalculateMonthlyPayment(principal, annualRate, termMonths);
        var monthlyRate = annualRate / 12;
        var balance = principal;

        for (int i = 1; i <= termMonths; i++)
        {
            var interestAmount = Math.Round(balance * monthlyRate, 2);
            var principalAmount = Math.Min(monthlyPayment - interestAmount, balance);
            balance -= principalAmount;

            // Handle final payment rounding
            if (i == termMonths && balance != 0)
            {
                principalAmount += balance;
                balance = 0;
            }

            schedule.Add(new AmortizationEntry
            {
                PaymentNumber = i,
                DueDate = startDate.AddMonths(i),
                TotalPayment = monthlyPayment,
                PrincipalAmount = principalAmount,
                InterestAmount = interestAmount,
                BalanceAfter = balance
            });
        }

        return schedule;
    }

    #endregion

    #region Payment Operations

    /// <summary>
    /// Generate payment schedule records for a loan.
    /// </summary>
    private async Task GeneratePaymentScheduleAsync(int loanId, string username)
    {
        var loan = await this.GetLoanByIdAsync(loanId);
        if (loan == null)
            return;

        var schedule = this.GenerateAmortizationSchedule(
            loan.PrincipalAmount,
            loan.AnnualInterestRate,
            loan.TermMonths,
            loan.StartDate);

        using var session = this.Context.OpenSession(username);

        foreach (var entry in schedule)
        {
            var payment = new AssetLoanPayment
            {
                AssetLoanId = loanId,
                PaymentNumber = entry.PaymentNumber,
                DueDate = entry.DueDate,
                TotalAmount = entry.TotalPayment,
                PrincipalAmount = entry.PrincipalAmount,
                InterestAmount = entry.InterestAmount,
                BalanceAfter = entry.BalanceAfter,
                Status = LoanPaymentStatus.Pending
            };
            session.Attach(payment);
        }

        await session.SubmitChanges("GeneratePaymentSchedule");
    }

    /// <summary>
    /// Get payments for a loan.
    /// </summary>
    public async Task<List<AssetLoanPayment>> GetPaymentsAsync(int loanId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<AssetLoanPayment>()
                .Where(p => p.AssetLoanId == loanId)
                .OrderBy(p => p.PaymentNumber),
            page: 1, size: 500, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Record a loan payment.
    /// </summary>
    public async Task<SubmitOperation> RecordPaymentAsync(
        int paymentId,
        DateTimeOffset paidDate,
        string username)
    {
        var payment = await this.Context.LoadOneAsync<AssetLoanPayment>(
            p => p.AssetLoanPaymentId == paymentId);

        if (payment == null)
            return SubmitOperation.CreateFailure("Payment not found");

        var loan = await this.GetLoanByIdAsync(payment.AssetLoanId);
        if (loan == null)
            return SubmitOperation.CreateFailure("Loan not found");

        // Update payment
        payment.PaidDate = paidDate;
        payment.Status = paidDate <= payment.DueDate
            ? LoanPaymentStatus.Paid
            : LoanPaymentStatus.Late;

        // Update loan
        loan.PaymentsMade++;
        loan.TotalPrincipalPaid += payment.PrincipalAmount;
        loan.TotalInterestPaid += payment.InterestAmount;
        loan.RemainingPrincipal = payment.BalanceAfter;

        // Check if loan is paid off
        if (loan.RemainingPrincipal <= 0)
        {
            loan.Status = LoanStatus.PaidOff;
            loan.NextPaymentDue = null;
        }
        else
        {
            // Find next pending payment
            var payments = await this.GetPaymentsAsync(loan.AssetLoanId);
            var nextPayment = payments
                .Where(p => p.Status == LoanPaymentStatus.Pending)
                .OrderBy(p => p.DueDate)
                .FirstOrDefault();

            loan.NextPaymentDue = nextPayment?.DueDate;
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(payment);
        session.Attach(loan);
        var result = await session.SubmitChanges("RecordPayment");

        // Create interest expense if successful
        if (result.Success && payment.InterestAmount > 0)
        {
            await this.ExpenseService.CreateFromLoanPaymentAsync(
                loan.AssetId,
                payment.AssetLoanPaymentId,
                payment.InterestAmount,
                paidDate,
                $"Loan interest - Payment #{payment.PaymentNumber}",
                username);
        }

        return result;
    }

    /// <summary>
    /// Get next payment due for a loan.
    /// </summary>
    public async Task<AssetLoanPayment?> GetNextPaymentDueAsync(int loanId)
    {
        var payments = await this.GetPaymentsAsync(loanId);
        return payments
            .Where(p => p.Status == LoanPaymentStatus.Pending)
            .OrderBy(p => p.DueDate)
            .FirstOrDefault();
    }

    #endregion

    #region Dashboard Support

    /// <summary>
    /// Get count of overdue payments.
    /// </summary>
    public async Task<int> GetOverduePaymentsCountAsync()
    {
        var now = DateTimeOffset.Now;
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<AssetLoanPayment>()
                .Where(p => p.Status == LoanPaymentStatus.Pending)
                .Where(p => p.DueDate < now),
            page: 1, size: 10000, includeTotalRows: false);

        // Mark late payments
        foreach (var payment in result.ItemCollection.ToList())
        {
            payment.Status = LoanPaymentStatus.Missed;
        }

        return result.ItemCollection.Count;
    }

    /// <summary>
    /// Get count of upcoming payments in N days.
    /// </summary>
    public async Task<int> GetUpcomingPaymentsCountAsync(int daysAhead)
    {
        var now = DateTimeOffset.Now;
        var endDate = now.AddDays(daysAhead);

        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<AssetLoanPayment>()
                .Where(p => p.Status == LoanPaymentStatus.Pending)
                .Where(p => p.DueDate >= now)
                .Where(p => p.DueDate <= endDate),
            page: 1, size: 10000, includeTotalRows: false);

        return result.ItemCollection.Count;
    }

    /// <summary>
    /// Get overdue payments.
    /// </summary>
    public async Task<List<AssetLoanPayment>> GetOverduePaymentsAsync()
    {
        var now = DateTimeOffset.Now;
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<AssetLoanPayment>()
                .Where(p => p.Status == LoanPaymentStatus.Pending || p.Status == LoanPaymentStatus.Missed)
                .Where(p => p.DueDate < now)
                .OrderBy(p => p.DueDate),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Get upcoming payments.
    /// </summary>
    public async Task<List<AssetLoanPayment>> GetUpcomingPaymentsAsync(int daysAhead)
    {
        var now = DateTimeOffset.Now;
        var endDate = now.AddDays(daysAhead);

        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<AssetLoanPayment>()
                .Where(p => p.Status == LoanPaymentStatus.Pending)
                .Where(p => p.DueDate >= now)
                .Where(p => p.DueDate <= endDate)
                .OrderBy(p => p.DueDate),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    #endregion

    #region Reporting

    /// <summary>
    /// Get loan summary.
    /// </summary>
    public async Task<LoanSummary> GetLoanSummaryAsync()
    {
        var loansResult = await this.GetLoansAsync(page: 1, size: 10000);
        var loans = loansResult.ItemCollection;

        var activeLoans = loans.Where(l => l.Status == LoanStatus.Active).ToList();

        return new LoanSummary
        {
            TotalLoans = loans.Count,
            ActiveLoans = activeLoans.Count,
            PaidOffLoans = loans.Count(l => l.Status == LoanStatus.PaidOff),
            DefaultedLoans = loans.Count(l => l.Status == LoanStatus.Defaulted),
            TotalPrincipal = activeLoans.Sum(l => l.PrincipalAmount),
            RemainingPrincipal = activeLoans.Sum(l => l.RemainingPrincipal),
            TotalInterestPaid = loans.Sum(l => l.TotalInterestPaid),
            TotalPrincipalPaid = loans.Sum(l => l.TotalPrincipalPaid)
        };
    }

    /// <summary>
    /// Get loan details with payment history.
    /// </summary>
    public async Task<LoanDetails?> GetLoanDetailsAsync(int loanId)
    {
        var loan = await this.GetLoanByIdAsync(loanId);
        if (loan == null)
            return null;

        var payments = await this.GetPaymentsAsync(loanId);

        return new LoanDetails
        {
            Loan = loan,
            Payments = payments,
            PaidPayments = payments.Count(p => p.Status == LoanPaymentStatus.Paid || p.Status == LoanPaymentStatus.Late),
            PendingPayments = payments.Count(p => p.Status == LoanPaymentStatus.Pending),
            MissedPayments = payments.Count(p => p.Status == LoanPaymentStatus.Missed),
            ProgressPercent = loan.PrincipalAmount > 0
                ? (loan.TotalPrincipalPaid / loan.PrincipalAmount * 100)
                : 0
        };
    }

    #endregion
}

#region DTOs

/// <summary>
/// Amortization schedule entry.
/// </summary>
public class AmortizationEntry
{
    public int PaymentNumber { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal BalanceAfter { get; set; }
}

/// <summary>
/// Loan summary for dashboard.
/// </summary>
public class LoanSummary
{
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int PaidOffLoans { get; set; }
    public int DefaultedLoans { get; set; }
    public decimal TotalPrincipal { get; set; }
    public decimal RemainingPrincipal { get; set; }
    public decimal TotalInterestPaid { get; set; }
    public decimal TotalPrincipalPaid { get; set; }
}

/// <summary>
/// Loan details with payment history.
/// </summary>
public class LoanDetails
{
    public AssetLoan Loan { get; set; } = null!;
    public List<AssetLoanPayment> Payments { get; set; } = [];
    public int PaidPayments { get; set; }
    public int PendingPayments { get; set; }
    public int MissedPayments { get; set; }
    public decimal ProgressPercent { get; set; }
}

#endregion
