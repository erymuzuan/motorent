using MotoRent.Client.Controls;
using MotoRent.Client.Services;
using MotoRent.Domain.Entities;
using MotoRent.Services;

namespace MotoRent.Client.Pages.Manager;

public partial class DailyClose : LocalizedComponentBase<DailyClose>
{
    private bool m_loading = true;
    private bool m_processing;
    private int m_shopId;
    private DateTime m_selectedDate = DateTime.Today;
    private DailyTillSummary? m_summary;
    private Domain.Entities.DailyClose? m_dailyClose;
    private List<ShortageLog> m_shortages = [];

    private List<TillSessionSummary> UnresolvedSessions =>
        m_summary?.Sessions
            .Where(s => s.Status == TillSessionStatus.ClosedWithVariance && !s.IsVerified)
            .ToList() ?? [];

    protected override async Task OnInitializedAsync()
    {
        var shops = await ShopService.GetShopsAsync(page: 1, pageSize: 1);
        if (shops.ItemCollection.Any())
        {
            m_shopId = shops.ItemCollection.First().ShopId;
        }

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            m_loading = true;

            if (m_shopId > 0)
            {
                m_summary = await TillService.GetDailySummaryAsync(m_shopId, m_selectedDate);
                m_dailyClose = await TillService.GetDailyCloseAsync(m_shopId, m_selectedDate);
                m_shortages = await TillService.GetShortageLogsAsync(m_shopId, m_selectedDate, m_selectedDate);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading daily close data");
            ShowError(Localizer["LoadDataFailed"]);
        }
        finally
        {
            m_loading = false;
        }
    }

    private async Task CloseDayAsync()
    {
        if (m_summary == null) return;

        // Check for unverified sessions with variance
        var unverifiedWithVariance = m_summary.Sessions
            .Where(s => s.Status == TillSessionStatus.ClosedWithVariance && !s.IsVerified)
            .Count();

        var message = Localizer["CloseDayConfirm", m_selectedDate.ToString("dd MMM yyyy")].ToString();
        if (unverifiedWithVariance > 0)
        {
            message += "\n\n" + Localizer["UnverifiedVariancesWarning", unverifiedWithVariance];
        }

        var confirmed = await DialogService.ConfirmYesNoAsync(message, Localizer["CloseDay"]);
        if (!confirmed) return;

        try
        {
            m_processing = true;
            var result = await TillService.PerformDailyCloseAsync(m_shopId, m_selectedDate, UserName);
            if (result.Success)
            {
                ShowSuccess(Localizer["DayClosed"]);
                await LoadDataAsync();
            }
            else
            {
                ShowError(result.Message ?? Localizer["CloseDayFailed"]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error closing day");
            ShowError(Localizer["CloseDayFailed"]);
        }
        finally
        {
            m_processing = false;
        }
    }

    private async Task OpenReopenDialogAsync()
    {
        var reason = await DialogService.PromptAsync(
            Localizer["ReopenDayPrompt"],
            Localizer["ReopenDay"]);

        if (string.IsNullOrWhiteSpace(reason)) return;

        if (reason.Length < 10)
        {
            ShowWarning(Localizer["ReasonTooShort"]);
            return;
        }

        try
        {
            m_processing = true;
            var result = await TillService.ReopenDayAsync(m_shopId, m_selectedDate, reason, UserName);
            if (result.Success)
            {
                ShowSuccess(Localizer["DayReopened"]);
                await LoadDataAsync();
            }
            else
            {
                ShowError(result.Message ?? Localizer["ReopenDayFailed"]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reopening day");
            ShowError(Localizer["ReopenDayFailed"]);
        }
        finally
        {
            m_processing = false;
        }
    }

    private async Task OpenShortageDialogAsync(TillSessionSummary session)
    {
        var result = await DialogService.Create<ShortageLogDialog>(Localizer["PostToShortage"])
            .WithParameter(x => x.SessionId, session.TillSessionId)
            .WithParameter(x => x.StaffUserName, GetStaffUserNameFromSession(session))
            .WithParameter(x => x.StaffDisplayName, session.StaffDisplayName)
            .WithParameter(x => x.Variance, session.Variance)
            .WithParameter(x => x.DailyCloseId, m_dailyClose?.DailyCloseId)
            .WithSize(ModalSize.Medium)
            .ShowDialogAsync();

        if (result is { Cancelled: false })
        {
            ShowSuccess(Localizer["ShortageLogged"]);
            await LoadDataAsync();
        }
    }

    private string GetStaffUserNameFromSession(TillSessionSummary session)
    {
        // Get the full session to extract the username
        // For now, we'll use display name as a fallback
        return session.StaffDisplayName;
    }

    private async Task ViewSessionDetailsAsync(TillSessionSummary session)
    {
        await DialogService.Create<EodSessionDetailDialog>(Localizer["SessionDetails"])
            .WithParameter(x => x.SessionId, session.TillSessionId)
            .WithSize(ModalSize.Large)
            .ShowDialogAsync();
    }

    private Task OpenSummaryReportAsync()
    {
        // DailySummaryReportDialog will be implemented in Task 2
        return Task.CompletedTask;
    }

    #region Helper Methods

    private static string GetStatusAvatarClass(DailyCloseStatus status) => status switch
    {
        DailyCloseStatus.Open => "bg-primary-lt",
        DailyCloseStatus.Closed => "bg-success-lt",
        DailyCloseStatus.Reconciled => "bg-success",
        _ => "bg-secondary"
    };

    private static string GetStatusIcon(DailyCloseStatus status) => status switch
    {
        DailyCloseStatus.Open => "ti ti-lock-open",
        DailyCloseStatus.Closed => "ti ti-lock",
        DailyCloseStatus.Reconciled => "ti ti-circle-check",
        _ => "ti ti-help"
    };

    private static string GetStatusBadgeClass(DailyCloseStatus status) => status switch
    {
        DailyCloseStatus.Open => "bg-primary",
        DailyCloseStatus.Closed => "bg-success",
        DailyCloseStatus.Reconciled => "bg-success-lt text-success",
        _ => "bg-secondary"
    };

    private string GetStatusText(DailyCloseStatus status) => status switch
    {
        DailyCloseStatus.Open => Localizer["StatusOpen"],
        DailyCloseStatus.Closed => Localizer["StatusClosed"],
        DailyCloseStatus.Reconciled => Localizer["StatusReconciled"],
        _ => status.ToString()
    };

    private static string GetVarianceAvatarClass(decimal variance)
    {
        if (variance == 0) return "bg-success-lt";
        if (variance > 0) return "bg-info-lt";
        return "bg-danger-lt";
    }

    private static string GetVarianceTextClass(decimal variance)
    {
        if (variance == 0) return "text-success";
        if (variance > 0) return "text-info";
        return "text-danger";
    }

    #endregion
}
