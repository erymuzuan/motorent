using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Core;

/// <summary>
/// Service for managing user feedback - submitting, querying, and status management.
/// </summary>
public class FeedbackService
{
    private readonly CoreDataContext m_context;
    private readonly FeedbackEmailService m_emailService;

    public FeedbackService(CoreDataContext context, FeedbackEmailService emailService)
    {
        m_context = context;
        m_emailService = emailService;
    }

    #region Submit

    /// <summary>
    /// Submits a new feedback entry and fires an email notification.
    /// </summary>
    public async Task SubmitFeedbackAsync(Feedback feedback, string userName)
    {
        feedback.Timestamp = DateTimeOffset.Now;

        using var session = m_context.OpenSession(userName);
        session.Attach(feedback);
        await session.SubmitChanges("SubmitFeedback");

        // Fire-and-forget email notification (don't await to avoid blocking the UI)
        _ = Task.Run(async () =>
        {
            try
            {
                await m_emailService.SendFeedbackNotificationAsync(feedback);
            }
            catch
            {
                // Silently swallow - email is best-effort
            }
        });
    }

    #endregion

    #region Queries

    /// <summary>
    /// Gets feedback entries with filtering and pagination.
    /// </summary>
    public async Task<LoadOperation<Feedback>> GetFeedbacksAsync(FeedbackFilter filter, int page = 1, int size = 20)
    {
        var query = BuildQuery(filter);
        query = query.OrderByDescending(x => x.FeedbackId);

        return await m_context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Gets a single feedback entry by ID.
    /// </summary>
    public async Task<Feedback?> GetByIdAsync(int id)
    {
        return await m_context.LoadOneAsync<Feedback>(x => x.FeedbackId == id);
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Updates the status and admin notes of a feedback entry.
    /// </summary>
    public async Task UpdateStatusAsync(int id, FeedbackStatus status, string? adminNotes, string userName)
    {
        var feedback = await GetByIdAsync(id);
        if (feedback is null) return;

        feedback.Status = status;
        if (adminNotes is not null)
            feedback.AdminNotes = adminNotes;

        using var session = m_context.OpenSession(userName);
        session.Attach(feedback);
        await session.SubmitChanges("UpdateFeedbackStatus");
    }

    /// <summary>
    /// Deletes a feedback entry.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var feedback = await GetByIdAsync(id);
        if (feedback is null) return;

        using var session = m_context.OpenSession("system");
        session.Delete(feedback);
        await session.SubmitChanges("DeleteFeedback");
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Gets feedback statistics for dashboard display.
    /// </summary>
    public async Task<FeedbackStats> GetStatsAsync()
    {
        var allQuery = m_context.Feedbacks.AsQueryable();
        var allLo = await m_context.LoadAsync(allQuery, 1, 1, includeTotalRows: true);

        var newQuery = m_context.Feedbacks.Where(x => x.Status == FeedbackStatus.New);
        var newLo = await m_context.LoadAsync(newQuery, 1, 1, includeTotalRows: true);

        var errorQuery = m_context.Feedbacks.Where(x => x.Type == FeedbackType.ErrorReport);
        var errorLo = await m_context.LoadAsync(errorQuery, 1, 1, includeTotalRows: true);

        return new FeedbackStats(allLo.TotalRows, newLo.TotalRows, errorLo.TotalRows);
    }

    #endregion

    #region Private Helpers

    private IQueryable<Feedback> BuildQuery(FeedbackFilter filter)
    {
        var query = m_context.Feedbacks.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.Type.HasValue)
            query = query.Where(x => x.Type == filter.Type.Value);

        if (!string.IsNullOrEmpty(filter.AccountNo))
            query = query.Where(x => x.AccountNo != null && x.AccountNo.Contains(filter.AccountNo));

        if (!string.IsNullOrEmpty(filter.UserName))
            query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));

        if (!string.IsNullOrEmpty(filter.Search))
            query = query.Where(x => x.Description.Contains(filter.Search));

        return query;
    }

    #endregion
}

#region DTOs

/// <summary>
/// Filter criteria for feedback queries.
/// </summary>
public record FeedbackFilter(
    FeedbackStatus? Status = null,
    FeedbackType? Type = null,
    string? AccountNo = null,
    string? UserName = null,
    string? Search = null
);

/// <summary>
/// Statistics for feedback dashboard.
/// </summary>
public record FeedbackStats(
    int TotalCount,
    int NewCount,
    int ErrorReportCount
);

#endregion
