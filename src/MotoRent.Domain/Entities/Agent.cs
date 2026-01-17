namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents an agent (tour guide, hotel, travel agency) who makes bookings on behalf of customers.
/// Agents can earn commissions and optionally add surcharges to bookings.
/// </summary>
public class Agent : Entity
{
    public int AgentId { get; set; }

    /// <summary>
    /// Unique code for the agent (e.g., "TG-001", "HTL-PATONG").
    /// Used for quick lookup and booking attribution.
    /// </summary>
    public string AgentCode { get; set; } = string.Empty;

    /// <summary>
    /// Business or individual name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact person name.
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Business address.
    /// </summary>
    public string? Address { get; set; }

    // Type & Status

    /// <summary>
    /// Type of agent: TourGuide, Hotel, TravelAgency, Concierge, Resort, TransportCompany, Other.
    /// </summary>
    public string AgentType { get; set; } = Entities.AgentType.Other;

    /// <summary>
    /// Current status: Active, Inactive, Suspended.
    /// </summary>
    public string Status { get; set; } = AgentStatus.Active;

    // Commission Settings

    /// <summary>
    /// How commission is calculated: Percentage, FixedPerBooking, FixedPerVehicle, FixedPerDay.
    /// </summary>
    public string CommissionType { get; set; } = AgentCommissionType.Percentage;

    /// <summary>
    /// Commission rate value. For Percentage type, this is the percent (e.g., 10 for 10%).
    /// For Fixed types, this is the amount in THB.
    /// </summary>
    public decimal CommissionRate { get; set; }

    /// <summary>
    /// Minimum commission per booking (optional).
    /// </summary>
    public decimal? MinCommission { get; set; }

    /// <summary>
    /// Maximum commission cap per booking (optional).
    /// </summary>
    public decimal? MaxCommission { get; set; }

    // Surcharge Settings (markup agent adds to customer)

    /// <summary>
    /// Whether agent is allowed to add surcharges to bookings.
    /// </summary>
    public bool AllowSurcharge { get; set; }

    /// <summary>
    /// How surcharge is calculated: Percentage, FixedPerBooking, FixedPerVehicle, FixedPerDay.
    /// </summary>
    public string? SurchargeType { get; set; }

    /// <summary>
    /// Default surcharge rate for this agent.
    /// </summary>
    public decimal? DefaultSurchargeRate { get; set; }

    /// <summary>
    /// If true, surcharge is hidden from customer (shown as single total).
    /// If false, surcharge appears as a separate line item (e.g., "Service Fee").
    /// </summary>
    public bool SurchargeHiddenFromCustomer { get; set; }

    // Invoicing

    /// <summary>
    /// Whether agent can generate their own invoices for customers.
    /// </summary>
    public bool CanGenerateInvoice { get; set; }

    /// <summary>
    /// Prefix for agent-generated invoice numbers (e.g., "HTL-INV-").
    /// </summary>
    public string? InvoicePrefix { get; set; }

    /// <summary>
    /// Default notes to include on agent-generated invoices.
    /// </summary>
    public string? InvoiceNotes { get; set; }

    // Payment Details for Commission Payouts

    /// <summary>
    /// Bank name for commission payments.
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Bank account number.
    /// </summary>
    public string? BankAccountNo { get; set; }

    /// <summary>
    /// Account holder name.
    /// </summary>
    public string? BankAccountName { get; set; }

    /// <summary>
    /// PromptPay ID for payments.
    /// </summary>
    public string? PromptPayId { get; set; }

    // Statistics (denormalized for quick access)

    /// <summary>
    /// Total number of bookings made by this agent.
    /// </summary>
    public int TotalBookings { get; set; }

    /// <summary>
    /// Total commission earned across all bookings.
    /// </summary>
    public decimal TotalCommissionEarned { get; set; }

    /// <summary>
    /// Total commission paid to agent.
    /// </summary>
    public decimal TotalCommissionPaid { get; set; }

    /// <summary>
    /// Outstanding commission balance (Earned - Paid).
    /// </summary>
    public decimal CommissionBalance { get; set; }

    // Metadata

    /// <summary>
    /// Internal notes about the agent.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date of most recent booking by this agent.
    /// </summary>
    public DateTimeOffset? LastBookingDate { get; set; }

    public override int GetId() => AgentId;
    public override void SetId(int value) => AgentId = value;
}
