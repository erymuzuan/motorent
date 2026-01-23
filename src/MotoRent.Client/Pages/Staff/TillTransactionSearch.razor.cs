using MotoRent.Client.Components.Receipts;
using MotoRent.Domain.Entities;

namespace MotoRent.Client.Pages.Staff;

public partial class TillTransactionSearch
{
    private List<Receipt> m_receipts = [];
    private bool m_loading = true;

    // Filter fields
    private DateTime? m_fromDate;
    private DateTime? m_toDate;
    private string? m_receiptType;
    private string? m_searchTerm;
    private decimal? m_minAmount;
    private decimal? m_maxAmount;

    // Pagination
    private int m_currentPage = 1;
    private const int c_pageSize = 20;
    private int m_totalCount;
    private int m_totalPages;

    // Summary
    private decimal m_totalAmount;

    // Debounce timer for search input
    private System.Timers.Timer? m_debounceTimer;

    protected override async Task OnInitializedAsync()
    {
        // Default to today
        ApplyQuickDateInternal("today");
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        m_loading = true;
        try
        {
            var fromDateOffset = m_fromDate.HasValue
                ? new DateTimeOffset(m_fromDate.Value)
                : (DateTimeOffset?)null;
            var toDateOffset = m_toDate.HasValue
                ? new DateTimeOffset(m_toDate.Value.AddDays(1).AddSeconds(-1))
                : (DateTimeOffset?)null;

            var result = await ReceiptService.GetReceiptsAsync(
                shopId: 0, // Gets all shops for this tenant
                receiptType: m_receiptType,
                status: null, // Show all statuses (issued and voided)
                fromDate: fromDateOffset,
                toDate: toDateOffset,
                searchTerm: m_searchTerm,
                page: m_currentPage,
                pageSize: c_pageSize);

            var receipts = result.ItemCollection.ToList();

            // Apply amount range filter in memory if specified
            if (m_minAmount.HasValue && m_minAmount.Value > 0)
            {
                receipts = receipts.Where(r => r.GrandTotal >= m_minAmount.Value).ToList();
            }
            if (m_maxAmount.HasValue && m_maxAmount.Value > 0)
            {
                receipts = receipts.Where(r => r.GrandTotal <= m_maxAmount.Value).ToList();
            }

            m_receipts = receipts;
            m_totalCount = result.TotalRows;
            m_totalPages = (int)Math.Ceiling((double)m_totalCount / c_pageSize);

            // Calculate total amount from issued receipts on current page
            m_totalAmount = m_receipts
                .Where(r => r.Status == ReceiptStatus.Issued)
                .Sum(r => r.GrandTotal);
        }
        catch (Exception ex)
        {
            ShowError(Localizer["ErrorLoading", ex.Message]);
        }
        finally
        {
            m_loading = false;
        }
    }

    private async Task FilterChanged()
    {
        m_currentPage = 1;
        await LoadDataAsync();
    }

    private void OnSearchTermChanged()
    {
        // Debounce search input to avoid too many requests
        m_debounceTimer?.Stop();
        m_debounceTimer?.Dispose();

        m_debounceTimer = new System.Timers.Timer(300);
        m_debounceTimer.Elapsed += async (_, _) =>
        {
            m_debounceTimer?.Stop();
            await InvokeAsync(async () =>
            {
                m_currentPage = 1;
                await LoadDataAsync();
                StateHasChanged();
            });
        };
        m_debounceTimer.AutoReset = false;
        m_debounceTimer.Start();
    }

    private async Task ClearSearch()
    {
        m_searchTerm = null;
        await FilterChanged();
    }

    private async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > m_totalPages) return;
        m_currentPage = page;
        await LoadDataAsync();
    }

    private async Task ApplyQuickDate(string range)
    {
        ApplyQuickDateInternal(range);
        await FilterChanged();
    }

    private void ApplyQuickDateInternal(string range)
    {
        switch (range)
        {
            case "today":
                m_fromDate = DateTime.Today;
                m_toDate = DateTime.Today;
                break;
            case "7days":
                m_fromDate = DateTime.Today.AddDays(-6);
                m_toDate = DateTime.Today;
                break;
            case "month":
                m_fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                m_toDate = DateTime.Today;
                break;
        }
    }

    private bool IsQuickDateActive(string range)
    {
        var today = DateTime.Today;
        return range switch
        {
            "today" => m_fromDate == today && m_toDate == today,
            "7days" => m_fromDate == today.AddDays(-6) && m_toDate == today,
            "month" => m_fromDate == new DateTime(today.Year, today.Month, 1) && m_toDate == today,
            _ => false
        };
    }

    private async Task ViewPrintReceiptAsync(Receipt receipt)
    {
        await DialogService
            .Create<ReceiptPrintDialog>(GetTypeDisplay(receipt.ReceiptType))
            .WithParameter(x => x.Entity, receipt)
            .WithParameter(x => x.IsReprint, true)
            .ShowDialogAsync();

        // Reload to update reprint count
        await LoadDataAsync();
    }

    private static string GetTypeBadgeClass(string type) => type switch
    {
        ReceiptTypes.CheckIn => "bg-primary",
        ReceiptTypes.Settlement => "bg-success",
        ReceiptTypes.BookingDeposit => "bg-info",
        _ => "bg-secondary"
    };

    private string GetTypeDisplay(string type) => type switch
    {
        ReceiptTypes.CheckIn => Localizer["CheckIn"],
        ReceiptTypes.Settlement => Localizer["Settlement"],
        ReceiptTypes.BookingDeposit => Localizer["BookingDeposit"],
        _ => type
    };

    public void Dispose()
    {
        m_debounceTimer?.Stop();
        m_debounceTimer?.Dispose();
    }
}
