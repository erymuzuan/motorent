using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

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
        var rentalIds = await this.Context.GetDistinctAsync<Rental, int>(
            r => r.ShopId == shopId,
            r => r.RentalId);

        // Build query with all filters applied at SQL level
        var query = this.Context.CreateQuery<Payment>()
            .Where(p => rentalIds.IsInList(p.RentalId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(paymentType))
        {
            query = query.Where(p => p.PaymentType == paymentType);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            query = query.Where(p => p.PaymentMethod == paymentMethod);
        }

        if (rentalId.HasValue)
        {
            query = query.Where(p => p.RentalId == rentalId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.PaidOn >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.PaidOn <= toDate.Value);
        }

        query = query.OrderByDescending(p => p.PaymentId);

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<List<Payment>> GetPaymentsByRentalIdAsync(int rentalId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Payment>().Where(p => p.RentalId == rentalId),
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
        return await session.SubmitChanges("CreatePayment");
    }

    public async Task<SubmitOperation> UpdatePaymentAsync(Payment payment, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(payment);
        return await session.SubmitChanges("UpdatePayment");
    }

    public async Task<SubmitOperation> DeletePaymentAsync(Payment payment, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(payment);
        return await session.SubmitChanges("DeletePayment");
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
