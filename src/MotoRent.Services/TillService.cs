using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class TillService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    #region Session Management

    /// <summary>
    /// Opens a new till session for a staff member.
    /// </summary>
    public async Task<SubmitOperation> OpenSessionAsync(
        int shopId,
        string staffUserName,
        string staffDisplayName,
        decimal openingFloat,
        string? notes = null)
    {
        // Check if staff already has an open session at this shop
        var existingSession = await GetActiveSessionAsync(shopId, staffUserName);
        if (existingSession != null)
            return SubmitOperation.CreateFailure("You already have an open till session");

        var session = new TillSession
        {
            ShopId = shopId,
            StaffUserName = staffUserName,
            StaffDisplayName = staffDisplayName,
            Status = TillSessionStatus.Open,
            OpeningFloat = openingFloat,
            OpenedAt = DateTimeOffset.Now,
            OpeningNotes = notes
        };

        using var persistenceSession = Context.OpenSession(staffUserName);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("OpenTillSession");
    }

    /// <summary>
    /// Gets the active (open) session for a staff member at a shop.
    /// </summary>
    public async Task<TillSession?> GetActiveSessionAsync(int shopId, string staffUserName)
    {
        return await Context.LoadOneAsync<TillSession>(s =>
            s.ShopId == shopId &&
            s.StaffUserName == staffUserName &&
            s.Status == TillSessionStatus.Open);
    }

    /// <summary>
    /// Gets any active (open) session for a staff member across all shops.
    /// Used by header button to display till status.
    /// </summary>
    public async Task<TillSession?> GetActiveSessionForUserAsync(string staffUserName)
    {
        return await Context.LoadOneAsync<TillSession>(s =>
            s.StaffUserName == staffUserName &&
            s.Status == TillSessionStatus.Open);
    }

    /// <summary>
    /// Checks if a staff member can open a new session at a shop.
    /// Enforces one-till-per-staff-per-shop-per-day constraint.
    /// </summary>
    public async Task<(bool CanOpen, string? Reason)> CanOpenSessionAsync(int shopId, string staffUserName)
    {
        // Check if staff already has an open session anywhere
        var existingOpen = await GetActiveSessionForUserAsync(staffUserName);
        if (existingOpen != null)
        {
            return (false, $"You already have an open till session at shop {existingOpen.ShopId}. Close it first.");
        }

        // Check for same-day session at this shop (one till per staff per shop per day)
        var today = DateTimeOffset.Now.Date;
        var todayStart = new DateTimeOffset(today, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var existingToday = await Context.LoadOneAsync<TillSession>(s =>
            s.ShopId == shopId &&
            s.StaffUserName == staffUserName &&
            s.OpenedAt >= todayStart &&
            s.OpenedAt < todayEnd);

        if (existingToday != null)
        {
            return (false, "You already had a till session at this shop today.");
        }

        return (true, null);
    }

    /// <summary>
    /// Gets all active sessions at a shop.
    /// </summary>
    public async Task<List<TillSession>> GetActiveSessionsAsync(int shopId)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId && s.Status == TillSessionStatus.Open)
                .OrderByDescending(s => s.OpenedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    public async Task<TillSession?> GetSessionByIdAsync(int sessionId)
    {
        return await Context.LoadOneAsync<TillSession>(s => s.TillSessionId == sessionId);
    }

    /// <summary>
    /// Closes a till session with reconciliation.
    /// </summary>
    public async Task<SubmitOperation> CloseSessionAsync(
        int sessionId,
        decimal actualCash,
        string? notes,
        string username)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session == null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        if (session.StaffUserName != username)
            return SubmitOperation.CreateFailure("You can only close your own session");

        session.ActualCash = actualCash;
        session.Variance = actualCash - session.ExpectedCash;
        session.ClosedAt = DateTimeOffset.Now;
        session.ClosingNotes = notes;

        // Determine status based on variance
        if (session.Variance == 0)
            session.Status = TillSessionStatus.Closed;
        else
            session.Status = TillSessionStatus.ClosedWithVariance;

        using var persistenceSession = Context.OpenSession(username);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("CloseTillSession");
    }

    /// <summary>
    /// Gets session history for a shop with optional filters.
    /// </summary>
    public async Task<LoadOperation<TillSession>> GetSessionHistoryAsync(
        int shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? staffUserName = null,
        TillSessionStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = Context.CreateQuery<TillSession>()
            .Where(s => s.ShopId == shopId);

        if (fromDate.HasValue)
            query = query.Where(s => s.OpenedAt >= new DateTimeOffset(fromDate.Value));

        if (toDate.HasValue)
            query = query.Where(s => s.OpenedAt <= new DateTimeOffset(toDate.Value.AddDays(1)));

        if (!string.IsNullOrWhiteSpace(staffUserName))
            query = query.Where(s => s.StaffUserName == staffUserName);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        query = query.OrderByDescending(s => s.OpenedAt);

        return await Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    #endregion

    #region Transaction Recording

    /// <summary>
    /// Records a cash-in transaction.
    /// </summary>
    public async Task<SubmitOperation> RecordCashInAsync(
        int sessionId,
        TillTransactionType type,
        decimal amount,
        string description,
        string username,
        int? paymentId = null,
        int? depositId = null,
        int? rentalId = null,
        string? notes = null)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session == null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        var transaction = new TillTransaction
        {
            TillSessionId = sessionId,
            TransactionType = type,
            Direction = TillTransactionDirection.In,
            Amount = amount,
            Description = description,
            PaymentId = paymentId,
            DepositId = depositId,
            RentalId = rentalId,
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = username,
            Notes = notes
        };

        // Update session totals based on transaction type
        if (transaction.AffectsCash)
            session.TotalCashIn += amount;

        if (type == TillTransactionType.TopUp)
            session.TotalToppedUp += amount;
        else if (type == TillTransactionType.CardPayment)
            session.TotalCardPayments += amount;
        else if (type == TillTransactionType.BankTransfer)
            session.TotalBankTransfers += amount;
        else if (type == TillTransactionType.PromptPay)
            session.TotalPromptPay += amount;

        using var persistenceSession = Context.OpenSession(username);
        persistenceSession.Attach(transaction);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("RecordCashIn");
    }

    /// <summary>
    /// Records a payout (cash-out) transaction.
    /// </summary>
    public async Task<SubmitOperation> RecordPayoutAsync(
        int sessionId,
        TillTransactionType type,
        decimal amount,
        string description,
        string username,
        string? category = null,
        string? recipientName = null,
        string? receiptNumber = null,
        int? depositId = null,
        int? rentalId = null,
        string? notes = null)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session == null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        var transaction = new TillTransaction
        {
            TillSessionId = sessionId,
            TransactionType = type,
            Direction = TillTransactionDirection.Out,
            Amount = amount,
            Category = category,
            Description = description,
            RecipientName = recipientName,
            ReceiptNumber = receiptNumber,
            DepositId = depositId,
            RentalId = rentalId,
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = username,
            Notes = notes
        };

        // Update session totals
        if (transaction.AffectsCash)
            session.TotalCashOut += amount;

        if (type == TillTransactionType.Drop)
            session.TotalDropped += amount;

        using var persistenceSession = Context.OpenSession(username);
        persistenceSession.Attach(transaction);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("RecordPayout");
    }

    /// <summary>
    /// Records a cash drop to the safe.
    /// </summary>
    public async Task<SubmitOperation> RecordDropAsync(
        int sessionId,
        decimal amount,
        string username,
        string? notes = null)
    {
        return await RecordPayoutAsync(
            sessionId,
            TillTransactionType.Drop,
            amount,
            "Cash drop to safe",
            username,
            notes: notes);
    }

    /// <summary>
    /// Records a top-up from the safe.
    /// </summary>
    public async Task<SubmitOperation> RecordTopUpAsync(
        int sessionId,
        decimal amount,
        string username,
        string? notes = null)
    {
        return await RecordCashInAsync(
            sessionId,
            TillTransactionType.TopUp,
            amount,
            "Top-up from safe",
            username,
            notes: notes);
    }

    /// <summary>
    /// Gets all transactions for a session.
    /// </summary>
    public async Task<List<TillTransaction>> GetTransactionsAsync(int sessionId)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<TillTransaction>()
                .Where(t => t.TillSessionId == sessionId)
                .OrderByDescending(t => t.TransactionTime),
            page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets recent transactions for a session (last N).
    /// </summary>
    public async Task<List<TillTransaction>> GetRecentTransactionsAsync(int sessionId, int count = 10)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<TillTransaction>()
                .Where(t => t.TillSessionId == sessionId)
                .OrderByDescending(t => t.TransactionTime),
            page: 1, size: count, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    #endregion

    #region Integration Methods

    /// <summary>
    /// Records a rental payment to the till using tillSessionId directly.
    /// All payment methods are recorded (cash affects drawer, others for reporting).
    /// </summary>
    public async Task<SubmitOperation> RecordRentalPaymentToTillAsync(
        int tillSessionId,
        string paymentMethod,
        int? paymentId,
        int rentalId,
        decimal amount,
        string description,
        string recordedByUserName)
    {
        var type = paymentMethod.ToLower() switch
        {
            "cash" => TillTransactionType.RentalPayment,
            "card" => TillTransactionType.CardPayment,
            "banktransfer" => TillTransactionType.BankTransfer,
            "promptpay" => TillTransactionType.PromptPay,
            "qrcode" => TillTransactionType.PromptPay, // QRCode is typically PromptPay in Thailand
            _ => TillTransactionType.RentalPayment
        };

        return await RecordCashInAsync(
            tillSessionId,
            type,
            amount,
            description,
            recordedByUserName,
            paymentId: paymentId,
            rentalId: rentalId);
    }

    /// <summary>
    /// Records a deposit collection to the till using tillSessionId directly.
    /// Only cash deposits affect the drawer; card pre-auth is recorded for reporting.
    /// </summary>
    public async Task<SubmitOperation> RecordDepositToTillAsync(
        int tillSessionId,
        int? depositId,
        int rentalId,
        decimal amount,
        string description,
        string recordedByUserName)
    {
        return await RecordCashInAsync(
            tillSessionId,
            TillTransactionType.SecurityDeposit,
            amount,
            description,
            recordedByUserName,
            depositId: depositId,
            rentalId: rentalId);
    }

    /// <summary>
    /// Records a rental payment to the till (legacy method using shopId/staffUserName).
    /// Call this when processing cash/card payments during check-in/check-out.
    /// </summary>
    public async Task<SubmitOperation> RecordRentalPaymentToTillAsync(
        int shopId,
        string staffUserName,
        int paymentId,
        int rentalId,
        decimal amount,
        string description,
        string paymentMethod)
    {
        var session = await GetActiveSessionAsync(shopId, staffUserName);
        if (session == null)
            return SubmitOperation.CreateFailure("No active till session. Please open a session first.");

        var type = paymentMethod.ToLower() switch
        {
            "cash" => TillTransactionType.RentalPayment,
            "card" => TillTransactionType.CardPayment,
            "banktransfer" => TillTransactionType.BankTransfer,
            "promptpay" => TillTransactionType.PromptPay,
            _ => TillTransactionType.RentalPayment
        };

        return await RecordCashInAsync(
            session.TillSessionId,
            type,
            amount,
            description,
            staffUserName,
            paymentId: paymentId,
            rentalId: rentalId);
    }

    /// <summary>
    /// Records a deposit refund from the till.
    /// Call this when refunding deposits during check-out.
    /// </summary>
    public async Task<SubmitOperation> RecordDepositRefundFromTillAsync(
        int shopId,
        string staffUserName,
        int depositId,
        int rentalId,
        decimal amount,
        string description)
    {
        var session = await GetActiveSessionAsync(shopId, staffUserName);
        if (session == null)
            return SubmitOperation.CreateFailure("No active till session. Please open a session first.");

        return await RecordPayoutAsync(
            session.TillSessionId,
            TillTransactionType.DepositRefund,
            amount,
            description,
            staffUserName,
            depositId: depositId,
            rentalId: rentalId);
    }

    #endregion

    #region Manager EOD Methods

    /// <summary>
    /// Gets all sessions for a specific date for manager verification.
    /// </summary>
    public async Task<List<TillSession>> GetSessionsForVerificationAsync(int shopId, DateTime date)
    {
        var startOfDay = new DateTimeOffset(date.Date);
        var endOfDay = startOfDay.AddDays(1);

        var result = await Context.LoadAsync(
            Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId)
                .Where(s => s.OpenedAt >= startOfDay && s.OpenedAt < endOfDay)
                .OrderBy(s => s.OpenedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets the daily summary for EOD reconciliation.
    /// </summary>
    public async Task<DailyTillSummary> GetDailySummaryAsync(int shopId, DateTime date)
    {
        var sessions = await GetSessionsForVerificationAsync(shopId, date);

        var summary = new DailyTillSummary
        {
            Date = date,
            ShopId = shopId,
            TotalSessions = sessions.Count,
            VerifiedSessions = sessions.Count(s => s.Status == TillSessionStatus.Verified),
            SessionsWithVariance = sessions.Count(s => s.Status == TillSessionStatus.ClosedWithVariance),
            TotalCashIn = sessions.Sum(s => s.TotalCashIn),
            TotalCashOut = sessions.Sum(s => s.TotalCashOut),
            TotalCardPayments = sessions.Sum(s => s.TotalCardPayments),
            TotalBankTransfers = sessions.Sum(s => s.TotalBankTransfers),
            TotalPromptPay = sessions.Sum(s => s.TotalPromptPay),
            TotalDropped = sessions.Sum(s => s.TotalDropped),
            TotalVariance = sessions.Sum(s => s.Variance),
            Sessions = sessions.Select(s => new TillSessionSummary
            {
                TillSessionId = s.TillSessionId,
                StaffDisplayName = s.StaffDisplayName,
                OpeningFloat = s.OpeningFloat,
                TotalCashIn = s.TotalCashIn,
                TotalCashOut = s.TotalCashOut,
                TotalDropped = s.TotalDropped,
                TotalToppedUp = s.TotalToppedUp,
                ExpectedCash = s.ExpectedCash,
                ActualCash = s.ActualCash,
                Variance = s.Variance,
                Status = s.Status,
                IsVerified = s.Status == TillSessionStatus.Verified,
                OpenedAt = s.OpenedAt,
                ClosedAt = s.ClosedAt
            }).ToList()
        };

        return summary;
    }

    /// <summary>
    /// Verifies a session (manager approval).
    /// </summary>
    public async Task<SubmitOperation> VerifySessionAsync(
        int sessionId,
        string managerUserName,
        string? notes = null)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session == null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Closed && session.Status != TillSessionStatus.ClosedWithVariance)
            return SubmitOperation.CreateFailure("Session must be closed before verification");

        session.Status = TillSessionStatus.Verified;
        session.VerifiedByUserName = managerUserName;
        session.VerifiedAt = DateTimeOffset.Now;
        session.VerificationNotes = notes;

        using var persistenceSession = Context.OpenSession(managerUserName);
        persistenceSession.Attach(session);
        return await persistenceSession.SubmitChanges("VerifyTillSession");
    }

    /// <summary>
    /// Verifies an individual transaction.
    /// </summary>
    public async Task<SubmitOperation> VerifyTransactionAsync(
        int transactionId,
        string managerUserName)
    {
        var transaction = await Context.LoadOneAsync<TillTransaction>(t => t.TillTransactionId == transactionId);
        if (transaction == null)
            return SubmitOperation.CreateFailure("Transaction not found");

        transaction.IsVerified = true;
        transaction.VerifiedByUserName = managerUserName;
        transaction.VerifiedAt = DateTimeOffset.Now;

        using var persistenceSession = Context.OpenSession(managerUserName);
        persistenceSession.Attach(transaction);
        return await persistenceSession.SubmitChanges("VerifyTillTransaction");
    }

    /// <summary>
    /// Gets unverified cash drops for a date.
    /// </summary>
    public async Task<List<TillTransaction>> GetUnverifiedDropsAsync(int shopId, DateTime date)
    {
        var sessions = await GetSessionsForVerificationAsync(shopId, date);
        var sessionIds = sessions.Select(s => s.TillSessionId).ToList();

        if (!sessionIds.Any())
            return [];

        var result = await Context.LoadAsync(
            Context.CreateQuery<TillTransaction>()
                .Where(t => t.TransactionType == TillTransactionType.Drop)
                .Where(t => !t.IsVerified),
            page: 1, size: 1000, includeTotalRows: false);

        // Filter by session IDs (since we can't do IsInList in the query easily)
        return result.ItemCollection
            .Where(t => sessionIds.Contains(t.TillSessionId))
            .OrderBy(t => t.TransactionTime)
            .ToList();
    }

    /// <summary>
    /// Gets card/electronic payments summary for a date.
    /// </summary>
    public async Task<List<TillTransaction>> GetCardPaymentsSummaryAsync(int shopId, DateTime date)
    {
        var sessions = await GetSessionsForVerificationAsync(shopId, date);
        var sessionIds = sessions.Select(s => s.TillSessionId).ToList();

        if (!sessionIds.Any())
            return [];

        var nonCashTypes = new[]
        {
            TillTransactionType.CardPayment,
            TillTransactionType.BankTransfer,
            TillTransactionType.PromptPay
        };

        var result = await Context.LoadAsync(
            Context.CreateQuery<TillTransaction>()
                .Where(t => t.Direction == TillTransactionDirection.In),
            page: 1, size: 10000, includeTotalRows: false);

        return result.ItemCollection
            .Where(t => sessionIds.Contains(t.TillSessionId))
            .Where(t => nonCashTypes.Contains(t.TransactionType))
            .OrderBy(t => t.TransactionTime)
            .ToList();
    }

    #endregion

    #region Reports

    /// <summary>
    /// Gets a summary for a specific session.
    /// </summary>
    public async Task<TillSessionSummary?> GetSessionSummaryAsync(int sessionId)
    {
        var session = await GetSessionByIdAsync(sessionId);
        if (session == null)
            return null;

        return new TillSessionSummary
        {
            TillSessionId = session.TillSessionId,
            StaffDisplayName = session.StaffDisplayName,
            OpeningFloat = session.OpeningFloat,
            TotalCashIn = session.TotalCashIn,
            TotalCashOut = session.TotalCashOut,
            TotalDropped = session.TotalDropped,
            TotalToppedUp = session.TotalToppedUp,
            ExpectedCash = session.ExpectedCash,
            ActualCash = session.ActualCash,
            Variance = session.Variance,
            Status = session.Status,
            IsVerified = session.Status == TillSessionStatus.Verified,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt
        };
    }

    /// <summary>
    /// Gets daily report with aggregated totals.
    /// </summary>
    public async Task<DailyTillSummary> GetDailyReportAsync(int shopId, DateTime date)
    {
        return await GetDailySummaryAsync(shopId, date);
    }

    /// <summary>
    /// Gets sessions with variance in a date range.
    /// </summary>
    public async Task<List<TillSession>> GetSessionsWithVarianceAsync(
        int shopId,
        DateTime fromDate,
        DateTime toDate)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId)
                .Where(s => s.Status == TillSessionStatus.ClosedWithVariance)
                .Where(s => s.OpenedAt >= new DateTimeOffset(fromDate))
                .Where(s => s.OpenedAt <= new DateTimeOffset(toDate.AddDays(1)))
                .OrderByDescending(s => s.OpenedAt),
            page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    #endregion
}

#region DTOs

/// <summary>
/// Summary of a till session for display.
/// </summary>
public class TillSessionSummary
{
    public int TillSessionId { get; set; }
    public string StaffDisplayName { get; set; } = string.Empty;
    public decimal OpeningFloat { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalToppedUp { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }
    public TillSessionStatus Status { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

/// <summary>
/// Daily summary for EOD reconciliation.
/// </summary>
public class DailyTillSummary
{
    public DateTime Date { get; set; }
    public int ShopId { get; set; }
    public int TotalSessions { get; set; }
    public int VerifiedSessions { get; set; }
    public int SessionsWithVariance { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalCardPayments { get; set; }
    public decimal TotalBankTransfers { get; set; }
    public decimal TotalPromptPay { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalVariance { get; set; }
    public List<TillSessionSummary> Sessions { get; set; } = [];

    /// <summary>
    /// Total non-cash (electronic) payments.
    /// </summary>
    public decimal TotalElectronicPayments => TotalCardPayments + TotalBankTransfers + TotalPromptPay;

    /// <summary>
    /// Net cash movement (in - out - dropped).
    /// </summary>
    public decimal NetCashMovement => TotalCashIn - TotalCashOut - TotalDropped;
}

#endregion
