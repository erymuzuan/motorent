using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Reporting operations for till service.
/// </summary>
public partial class TillService
{
    /// <summary>
    /// Gets a summary for a specific session.
    /// </summary>
    public async Task<TillSessionSummary?> GetSessionSummaryAsync(int sessionId)
    {
        var session = await this.GetSessionByIdAsync(sessionId);
        if (session is null)
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
    public Task<DailyTillSummary> GetDailyReportAsync(int shopId, DateTime date) =>
        this.GetDailySummaryAsync(shopId, date);

    /// <summary>
    /// Gets sessions with variance in a date range.
    /// </summary>
    public async Task<List<TillSession>> GetSessionsWithVarianceAsync(
        int shopId,
        DateTime fromDate,
        DateTime toDate)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<TillSession>()
                .Where(s => s.ShopId == shopId
                    && s.Status == TillSessionStatus.ClosedWithVariance
                    && s.OpenedAt >= new DateTimeOffset(fromDate)
                    && s.OpenedAt <= new DateTimeOffset(toDate.AddDays(1)))
                .OrderByDescending(s => s.OpenedAt),
            page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }
}
