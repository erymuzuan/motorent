using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class PaymentService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    #region CRUD Operations

    public async Task<LoadOperation<Payment>> GetPaymentsAsync(
        int shopId,
        string? status = null,
        string? paymentType = null,
        string? paymentMethod = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int? rentalId = null,
        int page = 1,
        int pageSize = 20)
    {
        // Get rental IDs for this shop first
        var rentals = await this.Context.LoadAsync(
            this.Context.Rentals.Where(r => r.ShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        var rentalIds = rentals.ItemCollection.Select(r => r.RentalId).ToHashSet();

        // Load all payments and filter in memory (since Contains isn't supported in expression trees)
        var allPaymentsResult = await this.Context.LoadAsync(
            this.Context.Payments.OrderByDescending(p => p.PaymentId),
            page: 1, size: 10000, includeTotalRows: false);

        var payments = allPaymentsResult.ItemCollection
            .Where(p => rentalIds.Contains(p.RentalId));

        // Apply filters
        if (!string.IsNullOrWhiteSpace(status))
        {
            payments = payments.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentType))
        {
            payments = payments.Where(p => p.PaymentType == paymentType);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            payments = payments.Where(p => p.PaymentMethod == paymentMethod);
        }

        if (rentalId.HasValue)
        {
            payments = payments.Where(p => p.RentalId == rentalId.Value);
        }

        if (fromDate.HasValue)
        {
            payments = payments.Where(p => p.PaidOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            payments = payments.Where(p => p.PaidOn <= toDate.Value);
        }

        var filteredList = payments.ToList();
        var totalCount = filteredList.Count;

        // Apply pagination
        var pagedList = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new LoadOperation<Payment>
        {
            ItemCollection = pagedList,
            TotalRows = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<Payment>> GetPaymentsByRentalIdAsync(int rentalId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.Payments.Where(p => p.RentalId == rentalId),
            page: 1, size: 100, includeTotalRows: false);

        return result.ItemCollection.ToList();
    }

    public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
    {
        return await this.Context.LoadOneAsync<Payment>(p => p.PaymentId == paymentId);
    }

    public async Task<SubmitOperation> CreatePaymentAsync(Payment payment, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(payment);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdatePaymentAsync(Payment payment, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(payment);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeletePaymentAsync(Payment payment, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(payment);
        return await session.SubmitChanges("Delete");
    }

    #endregion

    #region Business Operations

    public async Task<SubmitOperation> RecordPaymentAsync(RecordPaymentRequest request, string username)
    {
        var payment = new Payment
        {
            RentalId = request.RentalId,
            PaymentType = request.PaymentType,
            PaymentMethod = request.PaymentMethod,
            Amount = request.Amount,
            Status = "Completed",
            TransactionRef = request.TransactionRef,
            PaidOn = request.PaidOn ?? DateTimeOffset.Now,
            Notes = request.Notes
        };

        return await this.CreatePaymentAsync(payment, username);
    }

    public async Task<SubmitOperation> RefundPaymentAsync(int paymentId, string reason, string username)
    {
        var payment = await this.GetPaymentByIdAsync(paymentId);
        if (payment == null)
            return SubmitOperation.CreateFailure("Payment not found");

        if (payment.Status == "Refunded")
            return SubmitOperation.CreateFailure("Payment already refunded");

        payment.Status = "Refunded";
        payment.Notes = (payment.Notes ?? "") + $"\nRefunded: {reason}";

        return await this.UpdatePaymentAsync(payment, username);
    }

    #endregion

    #region Reports & Analytics

    public async Task<PaymentSummary> GetPaymentSummaryAsync(int shopId, DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        var payments = await this.GetPaymentsAsync(shopId, fromDate: fromDate, toDate: toDate, pageSize: 10000);
        var completedPayments = payments.ItemCollection.Where(p => p.Status == "Completed").ToList();

        return new PaymentSummary
        {
            TotalAmount = completedPayments.Sum(p => p.Amount),
            TotalCount = completedPayments.Count,
            ByPaymentMethod = completedPayments
                .GroupBy(p => p.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount)),
            ByPaymentType = completedPayments
                .GroupBy(p => p.PaymentType)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount)),
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    public async Task<Dictionary<string, decimal>> GetDailyRevenueAsync(int shopId, DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        var payments = await this.GetPaymentsAsync(shopId, status: "Completed", fromDate: fromDate, toDate: toDate, pageSize: 10000);

        return payments.ItemCollection
            .GroupBy(p => p.PaidOn.Date.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
    }

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(int shopId)
    {
        var payments = await this.GetPaymentsAsync(shopId, pageSize: 10000);

        return payments.ItemCollection
            .GroupBy(p => p.Status ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion
}

#region DTOs

public class RecordPaymentRequest
{
    public int RentalId { get; set; }
    public string PaymentType { get; set; } = "Rental";
    public string PaymentMethod { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public string? TransactionRef { get; set; }
    public DateTimeOffset? PaidOn { get; set; }
    public string? Notes { get; set; }
}

public class PaymentSummary
{
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }
    public Dictionary<string, decimal> ByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> ByPaymentType { get; set; } = new();
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
}

#endregion
