using Microsoft.JSInterop;

namespace MotoRent.Client.Services.Offline;

/// <summary>
/// Service for detecting and monitoring network connectivity state.
/// Provides events for online/offline state changes.
/// </summary>
public class NetworkStateService : IAsyncDisposable
{
    private readonly IJSRuntime m_jsRuntime;
    private DotNetObjectReference<NetworkStateService>? m_dotNetRef;
    private bool m_isOnline = true;
    private bool m_initialized;

    /// <summary>
    /// Event raised when connectivity state changes.
    /// </summary>
    public event Action<bool>? OnConnectivityChanged;

    /// <summary>
    /// Current online status.
    /// </summary>
    public bool IsOnline => m_isOnline;

    /// <summary>
    /// Current offline status.
    /// </summary>
    public bool IsOffline => !m_isOnline;

    public NetworkStateService(IJSRuntime jsRuntime)
    {
        m_jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initialize the service and register for connectivity events.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (m_initialized) return;

        try
        {
            m_dotNetRef = DotNetObjectReference.Create(this);

            // Check initial state
            m_isOnline = await m_jsRuntime.InvokeAsync<bool>("MotoRentNetwork.isOnline");

            // Register for events
            await m_jsRuntime.InvokeVoidAsync("MotoRentNetwork.registerCallback", m_dotNetRef);

            m_initialized = true;
            Console.WriteLine($"[NetworkStateService] Initialized. Online: {m_isOnline}");
        }
        catch (JSException ex)
        {
            Console.WriteLine($"[NetworkStateService] Init error: {ex.Message}");
            // Assume online if we can't determine state
            m_isOnline = true;
        }
    }

    /// <summary>
    /// Called from JavaScript when connectivity state changes.
    /// </summary>
    [JSInvokable]
    public void OnStateChanged(bool isOnline)
    {
        if (m_isOnline != isOnline)
        {
            m_isOnline = isOnline;
            Console.WriteLine($"[NetworkStateService] Connectivity changed. Online: {isOnline}");
            OnConnectivityChanged?.Invoke(isOnline);
        }
    }

    /// <summary>
    /// Check actual connectivity by attempting a network request.
    /// More reliable than navigator.onLine.
    /// </summary>
    public async Task<bool> CheckConnectivityAsync()
    {
        try
        {
            var result = await m_jsRuntime.InvokeAsync<bool>("MotoRentNetwork.checkConnectivity");
            m_isOnline = result;
            return result;
        }
        catch
        {
            // If JS call fails, we're likely offline
            m_isOnline = false;
            return false;
        }
    }

    /// <summary>
    /// Get connection type if available (wifi, cellular, etc.)
    /// </summary>
    public async Task<string?> GetConnectionTypeAsync()
    {
        try
        {
            return await m_jsRuntime.InvokeAsync<string?>("MotoRentNetwork.getConnectionType");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get connection effective type (4g, 3g, 2g, slow-2g)
    /// </summary>
    public async Task<string?> GetEffectiveTypeAsync()
    {
        try
        {
            return await m_jsRuntime.InvokeAsync<string?>("MotoRentNetwork.getEffectiveType");
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (m_dotNetRef != null)
        {
            try
            {
                await m_jsRuntime.InvokeVoidAsync("MotoRentNetwork.unregisterCallback");
            }
            catch
            {
                // Ignore errors during disposal
            }

            m_dotNetRef.Dispose();
            m_dotNetRef = null;
        }
    }
}
