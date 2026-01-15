using System.Net.Http.Json;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Server.Controllers;

namespace MotoRent.Client.Services;

/// <summary>
/// Client-side service for managing maintenance alerts.
/// </summary>
public class AlertService(HttpClient http)
{
    private HttpClient Http { get; } = http;

    /// <summary>
    /// Gets active maintenance alerts for the current shop.
    /// </summary>
    public async Task<LoadOperation<MaintenanceAlert>> GetActiveAlertsAsync(int page = 1, int size = 40)
    {
        var url = $"api/maintenance-alerts?page={page}&size={size}";
        return await this.Http.GetFromJsonAsync<LoadOperation<MaintenanceAlert>>(url) 
               ?? new LoadOperation<MaintenanceAlert>([], 0);
    }

    /// <summary>
    /// Marks an alert as read.
    /// </summary>
    public async Task<bool> MarkAsReadAsync(int alertId)
    {
        var response = await this.Http.PostAsync($"api/maintenance-alerts/{alertId}/read", null);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Resolves an alert.
    /// </summary>
    public async Task<bool> ResolveAlertAsync(int alertId, string notes)
    {
        var request = new ResolveAlertRequest { Notes = notes };
        var response = await this.Http.PostAsJsonAsync($"api/maintenance-alerts/{alertId}/resolve", request);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Manually triggers alert generation (Admin only).
    /// </summary>
    public async Task<int> TriggerAlertsAsync()
    {
        var response = await this.Http.PostAsync("api/maintenance-alerts/trigger", null);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<TriggerResult>();
            return result?.AlertsCreated ?? 0;
        }
        return -1;
    }

    private class TriggerResult
    {
        public int AlertsCreated { get; set; }
    }
}
