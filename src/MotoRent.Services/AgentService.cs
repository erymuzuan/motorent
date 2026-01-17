using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing agents (tour guides, hotels, travel agencies).
/// </summary>
public class AgentService
{
    private readonly RentalDataContext m_context;

    public AgentService(RentalDataContext context)
    {
        m_context = context;
    }

    #region CRUD Methods

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    public async Task<Agent?> GetAgentByIdAsync(int agentId)
    {
        return await m_context.LoadOneAsync<Agent>(a => a.AgentId == agentId);
    }

    /// <summary>
    /// Gets an agent by code.
    /// </summary>
    public async Task<Agent?> GetAgentByCodeAsync(string agentCode)
    {
        return await m_context.LoadOneAsync<Agent>(a => a.AgentCode == agentCode);
    }

    /// <summary>
    /// Gets agents with filters.
    /// </summary>
    public async Task<LoadOperation<Agent>> GetAgentsAsync(
        string? status = null,
        string? agentType = null,
        string? search = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.CreateQuery<Agent>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(agentType))
        {
            query = query.Where(a => a.AgentType == agentType);
        }

        query = query.OrderBy(a => a.Name);

        return await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Gets all active agents for dropdown/selection.
    /// </summary>
    public async Task<List<Agent>> GetActiveAgentsAsync()
    {
        var query = m_context.CreateQuery<Agent>()
            .Where(a => a.Status == AgentStatus.Active)
            .OrderBy(a => a.Name);

        var result = await m_context.LoadAsync(query, 1, 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Creates a new agent.
    /// </summary>
    public async Task<SubmitOperation> CreateAgentAsync(Agent agent, string username)
    {
        // Generate agent code if not provided
        if (string.IsNullOrWhiteSpace(agent.AgentCode))
        {
            agent.AgentCode = await GenerateAgentCodeAsync(agent.AgentType);
        }

        // Check for duplicate code
        var existing = await GetAgentByCodeAsync(agent.AgentCode);
        if (existing != null)
        {
            return SubmitOperation.CreateFailure($"Agent code '{agent.AgentCode}' already exists");
        }

        using var session = m_context.OpenSession(username);
        session.Attach(agent);
        return await session.SubmitChanges("Create");
    }

    /// <summary>
    /// Updates an existing agent.
    /// </summary>
    public async Task<SubmitOperation> UpdateAgentAsync(Agent agent, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(agent);
        return await session.SubmitChanges("Update");
    }

    /// <summary>
    /// Checks if an agent can be deleted (no associated bookings or commissions).
    /// </summary>
    public async Task<(bool CanDelete, string? Reason)> CanDeleteAgentAsync(int agentId)
    {
        // Check for associated commissions
        var commissionQuery = m_context.CreateQuery<AgentCommission>()
            .Where(c => c.AgentId == agentId);
        var commissionCount = await m_context.GetCountAsync(commissionQuery);

        if (commissionCount > 0)
        {
            return (false, $"Agent has {commissionCount} commission record(s) and cannot be deleted.");
        }

        // Check for associated bookings
        var bookingQuery = m_context.CreateQuery<Booking>()
            .Where(b => b.AgentId == agentId);
        var bookingCount = await m_context.GetCountAsync(bookingQuery);

        if (bookingCount > 0)
        {
            return (false, $"Agent has {bookingCount} booking(s) and cannot be deleted.");
        }

        return (true, null);
    }

    /// <summary>
    /// Deletes an agent if it has no associated data.
    /// </summary>
    public async Task<SubmitOperation> DeleteAgentAsync(int agentId, string username)
    {
        var agent = await GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            return SubmitOperation.CreateFailure("Agent not found");
        }

        var (canDelete, reason) = await CanDeleteAgentAsync(agentId);
        if (!canDelete)
        {
            return SubmitOperation.CreateFailure(reason ?? "Agent cannot be deleted");
        }

        using var session = m_context.OpenSession(username);
        session.Delete(agent);
        return await session.SubmitChanges("Delete");
    }

    /// <summary>
    /// Generates a unique agent code based on type.
    /// Format: TG-001 (TourGuide), HTL-001 (Hotel), etc.
    /// </summary>
    public async Task<string> GenerateAgentCodeAsync(string agentType)
    {
        var prefix = GetAgentCodePrefix(agentType);

        // Find highest existing number for this prefix
        var query = m_context.CreateQuery<Agent>()
            .Where(a => a.AgentCode.StartsWith(prefix));

        var result = await m_context.LoadAsync(query, 1, 1000, includeTotalRows: false);
        var existingCodes = result.ItemCollection
            .Select(a => a.AgentCode)
            .ToList();

        var maxNumber = 0;
        foreach (var code in existingCodes)
        {
            var numberPart = code.Replace(prefix, "").TrimStart('-');
            if (int.TryParse(numberPart, out var num) && num > maxNumber)
            {
                maxNumber = num;
            }
        }

        return $"{prefix}-{(maxNumber + 1):D3}";
    }

    private static string GetAgentCodePrefix(string agentType)
    {
        return agentType switch
        {
            AgentType.TourGuide => "TG",
            AgentType.Hotel => "HTL",
            AgentType.TravelAgency => "TA",
            AgentType.Concierge => "CON",
            AgentType.Resort => "RST",
            AgentType.TransportCompany => "TRN",
            _ => "AGT"
        };
    }

    #endregion

    #region Commission Calculation

    /// <summary>
    /// Calculates commission amount for a booking based on agent settings.
    /// </summary>
    public decimal CalculateCommission(Agent agent, decimal bookingTotal, int vehicleCount, int days)
    {
        var commission = agent.CommissionType switch
        {
            AgentCommissionType.Percentage => bookingTotal * agent.CommissionRate / 100m,
            AgentCommissionType.FixedPerBooking => agent.CommissionRate,
            AgentCommissionType.FixedPerVehicle => agent.CommissionRate * vehicleCount,
            AgentCommissionType.FixedPerDay => agent.CommissionRate * days,
            _ => 0m
        };

        // Apply min/max constraints
        if (agent.MinCommission.HasValue && commission < agent.MinCommission.Value)
        {
            commission = agent.MinCommission.Value;
        }

        if (agent.MaxCommission.HasValue && commission > agent.MaxCommission.Value)
        {
            commission = agent.MaxCommission.Value;
        }

        return Math.Round(commission, 2);
    }

    /// <summary>
    /// Calculates surcharge amount for a booking based on agent settings.
    /// </summary>
    public decimal CalculateSurcharge(Agent agent, decimal bookingTotal, int vehicleCount, int days, decimal? customRate = null)
    {
        if (!agent.AllowSurcharge)
        {
            return 0m;
        }

        var rate = customRate ?? agent.DefaultSurchargeRate ?? 0m;
        if (rate <= 0)
        {
            return 0m;
        }

        var surchargeType = agent.SurchargeType ?? AgentCommissionType.Percentage;

        var surcharge = surchargeType switch
        {
            AgentCommissionType.Percentage => bookingTotal * rate / 100m,
            AgentCommissionType.FixedPerBooking => rate,
            AgentCommissionType.FixedPerVehicle => rate * vehicleCount,
            AgentCommissionType.FixedPerDay => rate * days,
            _ => 0m
        };

        return Math.Round(surcharge, 2);
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Updates agent statistics (call after commission changes).
    /// </summary>
    public async Task<SubmitOperation> UpdateAgentStatisticsAsync(int agentId, string username)
    {
        var agent = await GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            return SubmitOperation.CreateFailure("Agent not found");
        }

        // Get all commissions for this agent
        var commissionQuery = m_context.CreateQuery<AgentCommission>()
            .Where(c => c.AgentId == agentId);

        var commissionResult = await m_context.LoadAsync(commissionQuery, 1, 10000, includeTotalRows: true);
        var commissions = commissionResult.ItemCollection.ToList();

        // Calculate totals
        agent.TotalBookings = commissions.Count;
        agent.TotalCommissionEarned = commissions
            .Where(c => c.Status != AgentCommissionStatus.Voided)
            .Sum(c => c.CommissionAmount);
        agent.TotalCommissionPaid = commissions
            .Where(c => c.Status == AgentCommissionStatus.Paid)
            .Sum(c => c.CommissionAmount);
        agent.CommissionBalance = agent.TotalCommissionEarned - agent.TotalCommissionPaid;

        // Update last booking date
        var latestCommission = commissions
            .Where(c => c.Status != AgentCommissionStatus.Voided)
            .OrderByDescending(c => c.CreatedTimestamp)
            .FirstOrDefault();

        if (latestCommission != null)
        {
            agent.LastBookingDate = latestCommission.CreatedTimestamp;
        }

        using var session = m_context.OpenSession(username);
        session.Attach(agent);
        return await session.SubmitChanges("UpdateStatistics");
    }

    /// <summary>
    /// Gets agent statistics for a date range.
    /// </summary>
    public async Task<AgentStatistics> GetAgentStatisticsAsync(int agentId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var agent = await GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            return new AgentStatistics();
        }

        var query = m_context.CreateQuery<AgentCommission>()
            .Where(c => c.AgentId == agentId)
            .Where(c => c.Status != AgentCommissionStatus.Voided);

        if (from.HasValue)
        {
            query = query.Where(c => c.CreatedTimestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(c => c.CreatedTimestamp <= to.Value);
        }

        var result = await m_context.LoadAsync(query, 1, 10000, includeTotalRows: true);
        var commissions = result.ItemCollection.ToList();

        return new AgentStatistics
        {
            AgentId = agentId,
            AgentName = agent.Name,
            TotalBookings = commissions.Count,
            TotalCommissionEarned = commissions.Sum(c => c.CommissionAmount),
            TotalCommissionPaid = commissions.Where(c => c.Status == AgentCommissionStatus.Paid).Sum(c => c.CommissionAmount),
            PendingCommission = commissions.Where(c => c.Status == AgentCommissionStatus.Pending).Sum(c => c.CommissionAmount),
            ApprovedCommission = commissions.Where(c => c.Status == AgentCommissionStatus.Approved).Sum(c => c.CommissionAmount),
            TotalBookingValue = commissions.Sum(c => c.BookingTotal),
            AverageBookingValue = commissions.Count > 0 ? commissions.Average(c => c.BookingTotal) : 0,
            FromDate = from,
            ToDate = to
        };
    }

    #endregion
}

#region Models

/// <summary>
/// Agent statistics for reporting.
/// </summary>
public class AgentStatistics
{
    public int AgentId { get; set; }
    public string? AgentName { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalCommissionEarned { get; set; }
    public decimal TotalCommissionPaid { get; set; }
    public decimal PendingCommission { get; set; }
    public decimal ApprovedCommission { get; set; }
    public decimal TotalBookingValue { get; set; }
    public decimal AverageBookingValue { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }

    public decimal CommissionBalance => TotalCommissionEarned - TotalCommissionPaid;
}

#endregion
