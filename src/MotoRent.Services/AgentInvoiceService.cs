using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing agent-generated invoices.
/// Agents can create their own invoices for customers (with optional markup).
/// </summary>
public class AgentInvoiceService
{
    private readonly RentalDataContext m_context;
    private readonly AgentService m_agentService;

    public AgentInvoiceService(RentalDataContext context, AgentService agentService)
    {
        m_context = context;
        m_agentService = agentService;
    }

    #region CRUD Methods

    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    public async Task<AgentInvoice?> GetInvoiceByIdAsync(int invoiceId)
    {
        return await m_context.LoadOneAsync<AgentInvoice>(i => i.AgentInvoiceId == invoiceId);
    }

    /// <summary>
    /// Gets invoice for a booking.
    /// </summary>
    public async Task<AgentInvoice?> GetInvoiceByBookingAsync(int bookingId)
    {
        return await m_context.LoadOneAsync<AgentInvoice>(i => i.BookingId == bookingId);
    }

    /// <summary>
    /// Gets invoices with filters.
    /// </summary>
    public async Task<LoadOperation<AgentInvoice>> GetInvoicesAsync(
        int? agentId = null,
        string? paymentStatus = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.CreateQuery<AgentInvoice>();

        if (agentId.HasValue)
        {
            query = query.Where(i => i.AgentId == agentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = query.Where(i => i.PaymentStatus == paymentStatus);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= toDate.Value);
        }

        query = query.OrderByDescending(i => i.AgentInvoiceId);

        return await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Creates an invoice for an agent booking.
    /// </summary>
    public async Task<AgentInvoice> CreateInvoiceAsync(CreateAgentInvoiceRequest request, string username)
    {
        var agent = await m_agentService.GetAgentByIdAsync(request.AgentId);

        var invoiceNo = await GenerateInvoiceNoAsync(agent);

        var invoice = new AgentInvoice
        {
            AgentId = request.AgentId,
            BookingId = request.BookingId,
            InvoiceNo = invoiceNo,
            InvoiceDate = request.InvoiceDate ?? DateTimeOffset.UtcNow,
            DueDate = request.DueDate,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CustomerAddress = request.CustomerAddress,
            SubTotal = request.SubTotal,
            SurchargeAmount = request.SurchargeAmount,
            TotalAmount = request.SubTotal + request.SurchargeAmount,
            Currency = request.Currency ?? "THB",
            PaymentStatus = BookingPaymentStatus.Unpaid,
            Notes = request.Notes ?? agent?.InvoiceNotes,
            Terms = request.Terms,
            Items = request.Items,
            AgentName = agent?.Name,
            BookingRef = request.BookingRef
        };

        using var session = m_context.OpenSession(username);
        session.Attach(invoice);
        await session.SubmitChanges("Create");

        return invoice;
    }

    /// <summary>
    /// Updates an existing invoice.
    /// </summary>
    public async Task<SubmitOperation> UpdateInvoiceAsync(AgentInvoice invoice, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(invoice);
        return await session.SubmitChanges("Update");
    }

    /// <summary>
    /// Generates a unique invoice number for an agent.
    /// </summary>
    public async Task<string> GenerateInvoiceNoAsync(Agent? agent)
    {
        var prefix = agent?.InvoicePrefix ?? "INV-";
        var year = DateTime.UtcNow.Year.ToString();

        // Find highest existing number for this prefix and year
        var searchPrefix = $"{prefix}{year}";
        var query = m_context.CreateQuery<AgentInvoice>()
            .Where(i => i.InvoiceNo.StartsWith(searchPrefix));

        var result = await m_context.LoadAsync(query, 1, 1000, includeTotalRows: false);
        var existingNumbers = result.ItemCollection
            .Select(i => i.InvoiceNo)
            .ToList();

        var maxNumber = 0;
        foreach (var invoiceNo in existingNumbers)
        {
            var numberPart = invoiceNo.Replace(searchPrefix, "").TrimStart('-');
            if (int.TryParse(numberPart, out var num) && num > maxNumber)
            {
                maxNumber = num;
            }
        }

        return $"{prefix}{year}-{(maxNumber + 1):D4}";
    }

    #endregion

    #region Payment Methods

    /// <summary>
    /// Records a payment against an invoice.
    /// </summary>
    public async Task<SubmitOperation> RecordPaymentAsync(int invoiceId, decimal amount, string username)
    {
        var invoice = await GetInvoiceByIdAsync(invoiceId);
        if (invoice == null)
        {
            return SubmitOperation.CreateFailure("Invoice not found");
        }

        invoice.AmountPaid += amount;

        // Update payment status
        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.PaymentStatus = BookingPaymentStatus.FullyPaid;
        }
        else if (invoice.AmountPaid > 0)
        {
            invoice.PaymentStatus = BookingPaymentStatus.PartiallyPaid;
        }

        using var session = m_context.OpenSession(username);
        session.Attach(invoice);
        return await session.SubmitChanges("RecordPayment");
    }

    /// <summary>
    /// Marks invoice as paid in full.
    /// </summary>
    public async Task<SubmitOperation> MarkAsPaidAsync(int invoiceId, string username)
    {
        var invoice = await GetInvoiceByIdAsync(invoiceId);
        if (invoice == null)
        {
            return SubmitOperation.CreateFailure("Invoice not found");
        }

        invoice.AmountPaid = invoice.TotalAmount;
        invoice.PaymentStatus = BookingPaymentStatus.FullyPaid;

        using var session = m_context.OpenSession(username);
        session.Attach(invoice);
        return await session.SubmitChanges("MarkPaid");
    }

    #endregion

    #region Invoice Generation

    /// <summary>
    /// Creates invoice from a booking (for agents who can generate invoices).
    /// </summary>
    public async Task<AgentInvoice?> CreateInvoiceFromBookingAsync(int bookingId, string username)
    {
        var booking = await m_context.LoadOneAsync<Booking>(b => b.BookingId == bookingId);
        if (booking == null || !booking.IsAgentBooking || !booking.AgentId.HasValue)
        {
            return null;
        }

        var agent = await m_agentService.GetAgentByIdAsync(booking.AgentId.Value);
        if (agent == null || !agent.CanGenerateInvoice)
        {
            return null;
        }

        // Check if invoice already exists
        var existingInvoice = await GetInvoiceByBookingAsync(bookingId);
        if (existingInvoice != null)
        {
            return existingInvoice;
        }

        // Build invoice items from booking items
        var items = new List<AgentInvoiceItem>();
        foreach (var bookingItem in booking.Items)
        {
            items.Add(new AgentInvoiceItem
            {
                Description = $"{bookingItem.VehicleDisplayName} ({booking.Days} days)",
                Quantity = booking.Days,
                UnitPrice = bookingItem.DailyRate + bookingItem.InsuranceRate + bookingItem.AccessoriesTotal,
                Amount = bookingItem.ItemTotal
            });
        }

        // Add location fees if any
        if (booking.PickupLocationFee > 0)
        {
            items.Add(new AgentInvoiceItem
            {
                Description = $"Pickup: {booking.PickupLocationName}",
                Quantity = 1,
                UnitPrice = booking.PickupLocationFee,
                Amount = booking.PickupLocationFee
            });
        }

        if (booking.DropoffLocationFee > 0)
        {
            items.Add(new AgentInvoiceItem
            {
                Description = $"Dropoff: {booking.DropoffLocationName}",
                Quantity = 1,
                UnitPrice = booking.DropoffLocationFee,
                Amount = booking.DropoffLocationFee
            });
        }

        var request = new CreateAgentInvoiceRequest
        {
            AgentId = booking.AgentId.Value,
            BookingId = bookingId,
            BookingRef = booking.BookingRef,
            CustomerName = booking.CustomerName,
            CustomerEmail = booking.CustomerEmail,
            SubTotal = booking.TotalAmount,
            SurchargeAmount = booking.AgentSurcharge,
            Items = items
        };

        return await CreateInvoiceAsync(request, username);
    }

    #endregion
}

#region Models

/// <summary>
/// Request to create an agent invoice.
/// </summary>
public class CreateAgentInvoiceRequest
{
    public int AgentId { get; set; }
    public int BookingId { get; set; }
    public string? BookingRef { get; set; }
    public DateTimeOffset? InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal SubTotal { get; set; }
    public decimal SurchargeAmount { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public List<AgentInvoiceItem> Items { get; set; } = [];
}

#endregion
