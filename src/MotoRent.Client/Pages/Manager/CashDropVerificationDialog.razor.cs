using Microsoft.AspNetCore.Components;
using MotoRent.Client.Controls;
using MotoRent.Client.Services;
using MotoRent.Domain.Entities;
using MotoRent.Services;

namespace MotoRent.Client.Pages.Manager;

public partial class CashDropVerificationDialog
{
    [Parameter]
    public int SessionId { get; set; }

    [Parameter]
    public string StaffName { get; set; } = string.Empty;

    [Parameter]
    public decimal TotalDropped { get; set; }

    private bool m_loading = true;
    private bool m_saving;
    private List<TillTransaction> m_drops = [];
    private Dictionary<string, decimal> m_dropTotals = new();
    private Dictionary<string, DropVerification> m_verifications = new();

    protected override async Task OnParametersSetAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            m_loading = true;

            // Load drop totals by currency
            m_dropTotals = await TillService.GetDropTotalsByCurrencyAsync(SessionId);

            // Load individual drop transactions for timeline
            m_drops = await TillService.GetDropTransactionsAsync(SessionId);

            // Initialize verification state for each currency
            m_verifications.Clear();
            foreach (var currency in m_dropTotals.Keys)
            {
                m_verifications[currency] = new DropVerification
                {
                    Matches = true,
                    DroppedAmount = m_dropTotals[currency]
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading drop data");
            ShowError(Localizer["LoadDataFailed"]);
        }
        finally
        {
            m_loading = false;
        }
    }

    private void OnMatchesChanged(string currency, bool matches)
    {
        if (m_verifications.TryGetValue(currency, out var verification))
        {
            verification.Matches = matches;
            if (matches)
            {
                // Clear actual amount and reason when switching to "Matches"
                verification.ActualAmount = null;
                verification.Reason = null;
            }
            StateHasChanged();
        }
    }

    private void OnActualAmountChanged(string currency, decimal? amount)
    {
        if (m_verifications.TryGetValue(currency, out var verification))
        {
            verification.ActualAmount = amount;
            StateHasChanged();
        }
    }

    private void OnReasonChanged(string currency, string? reason)
    {
        if (m_verifications.TryGetValue(currency, out var verification))
        {
            verification.Reason = reason;
            StateHasChanged();
        }
    }

    private bool IsValid()
    {
        foreach (var kvp in m_verifications)
        {
            var v = kvp.Value;
            if (!v.Matches)
            {
                // Actual amount required when not matching
                if (!v.ActualAmount.HasValue)
                    return false;

                // Reason required when amounts differ
                if (string.IsNullOrWhiteSpace(v.Reason))
                    return false;
            }
        }
        return true;
    }

    private decimal GetTotalVariance()
    {
        decimal total = 0;
        foreach (var kvp in m_verifications)
        {
            total += kvp.Value.Variance;
        }
        return total;
    }

    private bool HasAnyVariance()
    {
        return m_verifications.Values.Any(v => !v.Matches && v.Variance != 0);
    }

    private void Cancel()
    {
        ModalService.Close(ModalResult.Cancel());
    }

    private async Task ConfirmVerificationAsync()
    {
        if (!IsValid() || m_saving)
            return;

        try
        {
            m_saving = true;

            // Log any variances as shortages
            // For now, just close the dialog with success
            // Variance logging can be added when the ShortageLog feature is integrated

            ShowSuccess(Localizer["VerificationComplete"]);
            ModalService.Close(ModalResult.Ok(true));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error confirming verification");
            ShowError(Localizer["VerificationFailed"]);
        }
        finally
        {
            m_saving = false;
        }
    }

    private static string GetVarianceClass(decimal variance)
    {
        if (variance == 0) return "text-success";
        if (variance > 0) return "text-info";
        return "text-danger";
    }

    private string GetVarianceText(decimal variance)
    {
        if (variance == 0) return Localizer["Balanced"];
        if (variance > 0) return Localizer["Over"];
        return Localizer["Short"];
    }

    private static string FormatAmount(decimal amount, string currency)
    {
        return currency == SupportedCurrencies.THB
            ? amount.ToString("N0")
            : amount.ToString("N2");
    }

    /// <summary>
    /// Internal class to track verification state per currency.
    /// </summary>
    private class DropVerification
    {
        public decimal DroppedAmount { get; set; }
        public bool Matches { get; set; } = true;
        public decimal? ActualAmount { get; set; }
        public string? Reason { get; set; }

        public decimal Variance => Matches ? 0 : (ActualAmount ?? 0) - DroppedAmount;
    }
}
