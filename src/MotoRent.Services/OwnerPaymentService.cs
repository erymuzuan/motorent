using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing owner payments and calculating amounts due.
/// </summary>
public class OwnerPaymentService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    #region Query Methods

    public async Task<LoadOperation<OwnerPayment>> GetPaymentsAsync(
        int? vehicleOwnerId = null,
        OwnerPaymentStatus? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = this.Context.CreateQuery<OwnerPayment>().AsQueryable();

        if (vehicleOwnerId.HasValue)
        {
            query = query.Where(p => p.VehicleOwnerId == vehicleOwnerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.RentalStartDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.RentalEndDate <= toDate.Value);
        }

        query = query.OrderByDescending(p => p.OwnerPaymentId);

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<OwnerPayment?> GetPaymentByIdAsync(int paymentId)
    {
        return await this.Context.LoadOneAsync<OwnerPayment>(p => p.OwnerPaymentId == paymentId);
    }

    public async Task<OwnerPayment?> GetPaymentByRentalIdAsync(int rentalId)
    {
        return await this.Context.LoadOneAsync<OwnerPayment>(p => p.RentalId == rentalId);
    }

    #endregion

    #region Payment Status Management

    public async Task<SubmitOperation> MarkAsPaidAsync(
        int paymentId,
        string paymentMethod,
        string? paymentRef,
        string? notes,
        string username)
    {
        var payment = await this.GetPaymentByIdAsync(paymentId);
        if (payment is null)
            return SubmitOperation.CreateFailure("Payment not found");

        if (payment.Status is OwnerPaymentStatus.Paid)
            return SubmitOperation.CreateFailure("Payment already marked as paid");

        if (payment.Status is OwnerPaymentStatus.Cancelled)
            return SubmitOperation.CreateFailure("Cannot mark cancelled payment as paid");

        payment.Status = OwnerPaymentStatus.Paid;
        payment.PaidOn = DateTimeOffset.Now;
        payment.PaymentMethod = paymentMethod;
        payment.PaymentRef = paymentRef;
        payment.Notes = notes;

        using var session = this.Context.OpenSession(username);
        session.Attach(payment);
        return await session.SubmitChanges("MarkPaid");
    }

    public async Task<SubmitOperation> CancelPaymentAsync(int paymentId, string reason, string username)
    {
        var payment = await this.GetPaymentByIdAsync(paymentId);
        if (payment is null)
            return SubmitOperation.CreateFailure("Payment not found");

        if (payment.Status is OwnerPaymentStatus.Paid)
            return SubmitOperation.CreateFailure("Cannot cancel a paid payment");

        if (payment.Status is OwnerPaymentStatus.Cancelled)
            return SubmitOperation.CreateFailure("Payment already cancelled");

        payment.Status = OwnerPaymentStatus.Cancelled;
        payment.Notes = string.IsNullOrWhiteSpace(payment.Notes)
            ? $"Cancelled: {reason}"
            : $"{payment.Notes}\nCancelled: {reason}";

        using var session = this.Context.OpenSession(username);
        session.Attach(payment);
        return await session.SubmitChanges("Cancel");
    }

    public async Task<SubmitOperation> UpdatePaymentNotesAsync(int paymentId, string notes, string username)
    {
        var payment = await this.GetPaymentByIdAsync(paymentId);
        if (payment is null)
            return SubmitOperation.CreateFailure("Payment not found");

        payment.Notes = notes;

        using var session = this.Context.OpenSession(username);
        session.Attach(payment);
        return await session.SubmitChanges("UpdateNotes");
    }

    #endregion

    #region Summary & Reports

    public async Task<OwnerPaymentSummary> GetOwnerSummaryAsync(
        int vehicleOwnerId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var payments = await this.GetPaymentsAsync(
            vehicleOwnerId: vehicleOwnerId,
            fromDate: fromDate,
            toDate: toDate,
            pageSize: 10000);

        var all = payments.ItemCollection;

        return new OwnerPaymentSummary
        {
            VehicleOwnerId = vehicleOwnerId,
            TotalPending = all.Where(p => p.Status is OwnerPaymentStatus.Pending).Sum(p => p.Amount),
            TotalPaid = all.Where(p => p.Status is OwnerPaymentStatus.Paid).Sum(p => p.Amount),
            TotalCancelled = all.Where(p => p.Status is OwnerPaymentStatus.Cancelled).Sum(p => p.Amount),
            PendingCount = all.Count(p => p.Status is OwnerPaymentStatus.Pending),
            PaidCount = all.Count(p => p.Status is OwnerPaymentStatus.Paid),
            CancelledCount = all.Count(p => p.Status is OwnerPaymentStatus.Cancelled),
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    public async Task<OwnerPaymentSummary> GetAllOwnersSummaryAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var payments = await this.GetPaymentsAsync(
            fromDate: fromDate,
            toDate: toDate,
            pageSize: 10000);

        var all = payments.ItemCollection;

        return new OwnerPaymentSummary
        {
            TotalPending = all.Where(p => p.Status is OwnerPaymentStatus.Pending).Sum(p => p.Amount),
            TotalPaid = all.Where(p => p.Status is OwnerPaymentStatus.Paid).Sum(p => p.Amount),
            TotalCancelled = all.Where(p => p.Status is OwnerPaymentStatus.Cancelled).Sum(p => p.Amount),
            PendingCount = all.Count(p => p.Status is OwnerPaymentStatus.Pending),
            PaidCount = all.Count(p => p.Status is OwnerPaymentStatus.Paid),
            CancelledCount = all.Count(p => p.Status is OwnerPaymentStatus.Cancelled),
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    #endregion
}

#region DTOs

public class OwnerPaymentSummary
{
    public int? VehicleOwnerId { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalCancelled { get; set; }
    public int PendingCount { get; set; }
    public int PaidCount { get; set; }
    public int CancelledCount { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}

#endregion
