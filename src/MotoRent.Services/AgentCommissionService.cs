using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing agent commissions.
/// Commission workflow: Created → Eligible (rental completed) → Approved → Paid
/// If booking cancelled: Voided
/// </summary>
public class AgentCommissionService
{
    private readonly RentalDataContext m_context;
    private readonly AgentService m_agentService;

    public AgentCommissionService(RentalDataContext context, AgentService agentService)
    {
        m_context = context;
        m_agentService = agentService;
    }

    #region CRUD Methods

    /// <summary>
    /// Gets a commission by ID.
    /// </summary>
    public async Task<AgentCommission?> GetCommissionByIdAsync(int commissionId)
    {
        return await m_context.LoadOneAsync<AgentCommission>(c => c.AgentCommissionId == commissionId);
    }

    /// <summary>
    /// Gets commission for a booking.
    /// </summary>
    public async Task<AgentCommission?> GetCommissionByBookingAsync(int bookingId)
    {
        return await m_context.LoadOneAsync<AgentCommission>(c => c.BookingId == bookingId);
    }

    /// <summary>
    /// Gets commissions with filters.
    /// </summary>
    public async Task<LoadOperation<AgentCommission>> GetCommissionsAsync(
        int? agentId = null,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.CreateQuery<AgentCommission>();

        if (agentId.HasValue)
        {
            query = query.Where(c => c.AgentId == agentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(c => c.CreatedTimestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(c => c.CreatedTimestamp <= toDate.Value);
        }

        query = query.OrderByDescending(c => c.AgentCommissionId);

        return await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Creates a commission record for an agent booking.
    /// Called when booking is created, but status is Pending (not yet eligible).
    /// </summary>
    public async Task<AgentCommission> CreateCommissionAsync(
        int agentId,
        int bookingId,
        string bookingRef,
        string customerName,
        decimal bookingTotal,
        decimal commissionAmount,
        string calculationType,
        decimal commissionRate,
        string username)
    {
        var agent = await m_agentService.GetAgentByIdAsync(agentId);

        var commission = new AgentCommission
        {
            AgentId = agentId,
            BookingId = bookingId,
            BookingRef = bookingRef,
            CustomerName = customerName,
            BookingTotal = bookingTotal,
            CommissionAmount = commissionAmount,
            CalculationType = calculationType,
            CommissionRate = commissionRate,
            Status = AgentCommissionStatus.Pending,
            AgentName = agent?.Name,
            AgentCode = agent?.AgentCode
        };

        using var session = m_context.OpenSession(username);
        session.Attach(commission);
        await session.SubmitChanges("CreateAgentCommission");

        return commission;
    }

    #endregion

    #region Workflow Methods

    /// <summary>
    /// Makes commission eligible for approval (called when rental is completed).
    /// </summary>
    public async Task<SubmitOperation> MakeEligibleAsync(int commissionId, int rentalId, string username)
    {
        var commission = await GetCommissionByIdAsync(commissionId);
        if (commission == null)
        {
            return SubmitOperation.CreateFailure("Commission not found");
        }

        if (commission.Status != AgentCommissionStatus.Pending)
        {
            return SubmitOperation.CreateFailure($"Cannot make commission eligible - current status: {commission.Status}");
        }

        commission.RentalId = rentalId;
        commission.EligibleDate = DateTimeOffset.UtcNow;
        // Status remains Pending - eligible commissions are still Pending but have RentalId set

        using var session = m_context.OpenSession(username);
        session.Attach(commission);
        return await session.SubmitChanges("MakeEligible");
    }

    /// <summary>
    /// Approves a commission for payment.
    /// </summary>
    public async Task<SubmitOperation> ApproveCommissionAsync(int commissionId, string username)
    {
        var commission = await GetCommissionByIdAsync(commissionId);
        if (commission == null)
        {
            return SubmitOperation.CreateFailure("Commission not found");
        }

        if (commission.Status != AgentCommissionStatus.Pending)
        {
            return SubmitOperation.CreateFailure($"Cannot approve commission - current status: {commission.Status}");
        }

        if (!commission.RentalId.HasValue)
        {
            return SubmitOperation.CreateFailure("Cannot approve commission - rental not completed");
        }

        commission.Status = AgentCommissionStatus.Approved;
        commission.ApprovedDate = DateTimeOffset.UtcNow;
        commission.ApprovedBy = username;

        using var session = m_context.OpenSession(username);
        session.Attach(commission);
        var result = await session.SubmitChanges("Approve");

        // Update agent statistics
        if (result.Success)
        {
            await m_agentService.UpdateAgentStatisticsAsync(commission.AgentId, username);
        }

        return result;
    }

    /// <summary>
    /// Approves multiple commissions for payment.
    /// </summary>
    public async Task<BulkOperationResult> ApproveCommissionsAsync(int[] commissionIds, string username)
    {
        var results = new BulkOperationResult();

        foreach (var id in commissionIds)
        {
            var result = await ApproveCommissionAsync(id, username);
            if (result.Success)
            {
                results.SuccessCount++;
            }
            else
            {
                results.FailedCount++;
                results.Errors.Add($"Commission {id}: {result.Message}");
            }
        }

        return results;
    }

    /// <summary>
    /// Pays a commission to the agent.
    /// </summary>
    public async Task<SubmitOperation> PayCommissionAsync(
        int commissionId,
        string paymentMethod,
        string? paymentReference,
        string username)
    {
        var commission = await GetCommissionByIdAsync(commissionId);
        if (commission == null)
        {
            return SubmitOperation.CreateFailure("Commission not found");
        }

        if (commission.Status != AgentCommissionStatus.Approved)
        {
            return SubmitOperation.CreateFailure($"Cannot pay commission - current status: {commission.Status}");
        }

        commission.Status = AgentCommissionStatus.Paid;
        commission.PaidDate = DateTimeOffset.UtcNow;
        commission.PaidBy = username;
        commission.PaymentMethod = paymentMethod;
        commission.PaymentReference = paymentReference;

        using var session = m_context.OpenSession(username);
        session.Attach(commission);
        var result = await session.SubmitChanges("Pay");

        // Update agent statistics
        if (result.Success)
        {
            await m_agentService.UpdateAgentStatisticsAsync(commission.AgentId, username);
        }

        return result;
    }

    /// <summary>
    /// Pays multiple commissions to agents.
    /// </summary>
    public async Task<BulkOperationResult> PayCommissionsAsync(
        int[] commissionIds,
        string paymentMethod,
        string? paymentReference,
        string username)
    {
        var results = new BulkOperationResult();

        foreach (var id in commissionIds)
        {
            var result = await PayCommissionAsync(id, paymentMethod, paymentReference, username);
            if (result.Success)
            {
                results.SuccessCount++;
            }
            else
            {
                results.FailedCount++;
                results.Errors.Add($"Commission {id}: {result.Message}");
            }
        }

        return results;
    }

    /// <summary>
    /// Voids a commission (called when booking is cancelled).
    /// </summary>
    public async Task<SubmitOperation> VoidCommissionAsync(int commissionId, string reason, string username)
    {
        var commission = await GetCommissionByIdAsync(commissionId);
        if (commission == null)
        {
            return SubmitOperation.CreateFailure("Commission not found");
        }

        if (commission.Status == AgentCommissionStatus.Paid)
        {
            return SubmitOperation.CreateFailure("Cannot void a paid commission - use refund process instead");
        }

        if (commission.Status == AgentCommissionStatus.Voided)
        {
            return SubmitOperation.CreateSuccess(); // Already voided
        }

        commission.Status = AgentCommissionStatus.Voided;
        commission.VoidedDate = DateTimeOffset.UtcNow;
        commission.VoidedReason = reason;

        using var session = m_context.OpenSession(username);
        session.Attach(commission);
        var result = await session.SubmitChanges("Void");

        // Update agent statistics
        if (result.Success)
        {
            await m_agentService.UpdateAgentStatisticsAsync(commission.AgentId, username);
        }

        return result;
    }

    #endregion

    #region Reporting

    /// <summary>
    /// Gets commission summary for reporting.
    /// </summary>
    public async Task<CommissionSummary> GetCommissionSummaryAsync(
        int? agentId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var query = m_context.CreateQuery<AgentCommission>();

        if (agentId.HasValue)
        {
            query = query.Where(c => c.AgentId == agentId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(c => c.CreatedTimestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(c => c.CreatedTimestamp <= toDate.Value);
        }

        var result = await m_context.LoadAsync(query, 1, 10000, includeTotalRows: true);
        var commissions = result.ItemCollection.ToList();

        return new CommissionSummary
        {
            TotalCommissions = commissions.Count,
            PendingCount = commissions.Count(c => c.Status == AgentCommissionStatus.Pending),
            ApprovedCount = commissions.Count(c => c.Status == AgentCommissionStatus.Approved),
            PaidCount = commissions.Count(c => c.Status == AgentCommissionStatus.Paid),
            VoidedCount = commissions.Count(c => c.Status == AgentCommissionStatus.Voided),
            PendingAmount = commissions.Where(c => c.Status == AgentCommissionStatus.Pending).Sum(c => c.CommissionAmount),
            ApprovedAmount = commissions.Where(c => c.Status == AgentCommissionStatus.Approved).Sum(c => c.CommissionAmount),
            PaidAmount = commissions.Where(c => c.Status == AgentCommissionStatus.Paid).Sum(c => c.CommissionAmount),
            TotalBookingValue = commissions.Where(c => c.Status != AgentCommissionStatus.Voided).Sum(c => c.BookingTotal),
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    /// <summary>
    /// Gets pending/approved commissions by agent for payout.
    /// </summary>
    public async Task<List<AgentPayoutSummary>> GetPayoutSummaryByAgentAsync()
    {
        var query = m_context.CreateQuery<AgentCommission>()
            .Where(c => c.Status == AgentCommissionStatus.Pending || c.Status == AgentCommissionStatus.Approved);

        var result = await m_context.LoadAsync(query, 1, 10000, includeTotalRows: false);
        var commissions = result.ItemCollection.ToList();

        return commissions
            .GroupBy(c => new { c.AgentId, c.AgentName, c.AgentCode })
            .Select(g => new AgentPayoutSummary
            {
                AgentId = g.Key.AgentId,
                AgentName = g.Key.AgentName ?? string.Empty,
                AgentCode = g.Key.AgentCode ?? string.Empty,
                PendingCount = g.Count(c => c.Status == AgentCommissionStatus.Pending),
                ApprovedCount = g.Count(c => c.Status == AgentCommissionStatus.Approved),
                PendingAmount = g.Where(c => c.Status == AgentCommissionStatus.Pending).Sum(c => c.CommissionAmount),
                ApprovedAmount = g.Where(c => c.Status == AgentCommissionStatus.Approved).Sum(c => c.CommissionAmount),
                TotalAmount = g.Sum(c => c.CommissionAmount)
            })
            .OrderByDescending(s => s.TotalAmount)
            .ToList();
    }

    #endregion
}

#region Models

/// <summary>
/// Summary of commissions for reporting.
/// </summary>
public class CommissionSummary
{
    public int TotalCommissions { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PaidCount { get; set; }
    public int VoidedCount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal TotalBookingValue { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }

    public decimal TotalOutstanding => PendingAmount + ApprovedAmount;
}

/// <summary>
/// Payout summary per agent.
/// </summary>
public class AgentPayoutSummary
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string AgentCode { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Result of bulk operations.
/// </summary>
public class BulkOperationResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = [];

    public bool AllSucceeded => FailedCount == 0;
    public int TotalCount => SuccessCount + FailedCount;
}

#endregion
