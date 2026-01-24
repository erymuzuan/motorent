using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Denomination counting operations for till service.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Saves a denomination count for a till session.
    /// For opening counts, validates session is open.
    /// For closing counts, populates expected balances and calculates variances.
    /// </summary>
    /// <param name="tillSessionId">Till session ID</param>
    /// <param name="countType">Opening or Closing count type</param>
    /// <param name="breakdowns">Currency denomination breakdowns</param>
    /// <param name="countedByUserName">Username of staff performing count</param>
    /// <param name="isFinal">Whether this is a finalized count (default true)</param>
    /// <param name="notes">Optional notes about the count</param>
    /// <returns>Submit operation result</returns>
    public async Task<SubmitOperation> SaveDenominationCountAsync(
        int tillSessionId,
        DenominationCountType countType,
        List<CurrencyDenominationBreakdown> breakdowns,
        string countedByUserName,
        bool isFinal = true,
        string? notes = null)
    {
        // Validate session exists and is open
        var session = await this.GetSessionByIdAsync(tillSessionId);
        if (session is null)
            return SubmitOperation.CreateFailure("Session not found");

        if (session.Status != TillSessionStatus.Open)
            return SubmitOperation.CreateFailure("Session is not open");

        // For closing counts, populate expected balances and variances
        if (countType == DenominationCountType.Closing)
        {
            foreach (var breakdown in breakdowns)
            {
                var expectedBalance = GetExpectedBalanceForCurrency(session, breakdown.Currency);
                breakdown.ExpectedBalance = expectedBalance;
                // Variance is computed automatically in the CurrencyDenominationBreakdown class
            }
        }

        // Calculate total in THB
        decimal totalInThb = 0;
        foreach (var breakdown in breakdowns)
        {
            if (breakdown.Currency == SupportedCurrencies.THB)
            {
                totalInThb += breakdown.Total;
            }
            else
            {
                // Convert foreign currency to THB (with shop fallback to org defaults)
                var conversion = await this.ExchangeRateService.ConvertToThbAsync(breakdown.Currency, breakdown.Total, session.ShopId);
                if (conversion is not null)
                {
                    totalInThb += conversion.ThbAmount;
                }
                else
                {
                    // If no rate, use raw amount (fallback)
                    totalInThb += breakdown.Total;
                }
            }
        }

        // Check for existing draft count of same type - overwrite if exists
        var existingCount = await this.GetDenominationCountAsync(tillSessionId, countType, includeDrafts: true);

        TillDenominationCount denominationCount;
        if (existingCount is { IsFinal: false })
        {
            // Update existing draft
            denominationCount = existingCount;
            denominationCount.CurrencyBreakdowns = breakdowns;
            denominationCount.CountedAt = DateTimeOffset.Now;
            denominationCount.CountedByUserName = countedByUserName;
            denominationCount.Notes = notes;
            denominationCount.TotalInThb = totalInThb;
            denominationCount.IsFinal = isFinal;
        }
        else if (existingCount is { IsFinal: true })
        {
            // Cannot overwrite a final count
            return SubmitOperation.CreateFailure($"A final {countType.ToString().ToLower()} count already exists for this session");
        }
        else
        {
            // Create new count
            denominationCount = new TillDenominationCount
            {
                TillSessionId = tillSessionId,
                CountType = countType,
                CountedAt = DateTimeOffset.Now,
                CountedByUserName = countedByUserName,
                Notes = notes,
                CurrencyBreakdowns = breakdowns,
                TotalInThb = totalInThb,
                IsFinal = isFinal
            };
        }

        using var persistenceSession = this.Context.OpenSession(countedByUserName);
        persistenceSession.Attach(denominationCount);
        return await persistenceSession.SubmitChanges("SaveDenominationCount");
    }

    /// <summary>
    /// Gets the denomination count for a session by type.
    /// Returns the most recent final count, or draft if no final exists.
    /// </summary>
    /// <param name="tillSessionId">Till session ID</param>
    /// <param name="countType">Opening or Closing count type</param>
    /// <param name="includeDrafts">Whether to include draft counts (default false)</param>
    /// <returns>Denomination count or null if not found</returns>
    public async Task<TillDenominationCount?> GetDenominationCountAsync(
        int tillSessionId,
        DenominationCountType countType,
        bool includeDrafts = false)
    {
        var query = this.Context.CreateQuery<TillDenominationCount>()
            .Where(d => d.TillSessionId == tillSessionId && d.CountType == countType);

        if (!includeDrafts)
        {
            query = query.Where(d => d.IsFinal == true);
        }

        // Get the most recent count
        query = query.OrderByDescending(d => d.CountedAt);

        return await this.Context.LoadOneAsync(query);
    }

    /// <summary>
    /// Gets all denomination counts for a session (for history view).
    /// </summary>
    /// <param name="tillSessionId">Till session ID</param>
    /// <returns>List of all denomination counts for the session</returns>
    public async Task<List<TillDenominationCount>> GetDenominationCountsAsync(int tillSessionId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillDenominationCount>()
                .Where(d => d.TillSessionId == tillSessionId)
                .OrderByDescending(d => d.CountedAt),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets the expected balance for a currency in a session.
    /// For THB: returns the THB currency balance from session.
    /// For foreign currencies: returns the foreign currency balance.
    /// </summary>
    private static decimal GetExpectedBalanceForCurrency(TillSession session, string currency) =>
        session.GetCurrencyBalance(currency);
}
