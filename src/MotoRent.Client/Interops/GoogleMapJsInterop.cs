using Microsoft.JSInterop;
using MotoRent.Domain.Spatial;

namespace MotoRent.Client.Interops;

/// <summary>
/// JavaScript interop for Google Maps functionality.
/// Provides map initialization, marker management, and location picking.
/// </summary>
public class GoogleMapJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> m_moduleTask;
    private bool m_disposed;

    public GoogleMapJsInterop(IJSRuntime jsRuntime)
    {
        m_moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./scripts/google-map.js").AsTask());
    }

    /// <summary>
    /// Check if Google Maps API is loaded.
    /// </summary>
    public async Task<bool> IsGoogleMapsLoadedAsync()
    {
        var module = await m_moduleTask.Value;
        return await module.InvokeAsync<bool>("isGoogleMapsLoaded");
    }

    /// <summary>
    /// Initialize map picker with optional initial location.
    /// </summary>
    /// <param name="elementId">The DOM element ID for the map container.</param>
    /// <param name="center">Initial center location (defaults to Phuket if null).</param>
    /// <param name="readOnly">If true, marker is not draggable.</param>
    public async Task InitPickerAsync(string elementId, LatLng? center = null, bool readOnly = false)
    {
        var module = await m_moduleTask.Value;
        var centerObj = center?.HasValue == true
            ? new { lat = center.Lat, lng = center.Lng }
            : null;
        await module.InvokeVoidAsync("initPicker", elementId, centerObj, readOnly);
    }

    /// <summary>
    /// Get the current picker location.
    /// </summary>
    public async Task<LatLng?> GetPickerLocationAsync()
    {
        var module = await m_moduleTask.Value;
        var result = await module.InvokeAsync<LocationResult?>("getPickerLocation");
        if (result is null) return null;
        return new LatLng(result.Lat, result.Lng);
    }

    /// <summary>
    /// Set the picker location.
    /// </summary>
    public async Task SetPickerLocationAsync(LatLng location)
    {
        var module = await m_moduleTask.Value;
        await module.InvokeVoidAsync("setPickerLocation", new { lat = location.Lat, lng = location.Lng });
    }

    /// <summary>
    /// Display multiple markers on map.
    /// </summary>
    public async Task DisplayMarkersAsync(string elementId, IEnumerable<MapMarker> markers)
    {
        var module = await m_moduleTask.Value;
        var markerData = markers.Select(m => new
        {
            lat = m.Lat,
            lng = m.Lng,
            label = m.Label,
            address = m.Address,
            href = m.Href
        }).ToArray();
        await module.InvokeVoidAsync("displayMarkers", elementId, markerData);
    }

    /// <summary>
    /// Display a single marker on a view-only map.
    /// </summary>
    public async Task DisplaySingleMarkerAsync(string elementId, LatLng location, string? label = null)
    {
        var module = await m_moduleTask.Value;
        await module.InvokeVoidAsync("displaySingleMarker", elementId, new
        {
            lat = location.Lat,
            lng = location.Lng,
            label
        });
    }

    /// <summary>
    /// Get user's current GPS location.
    /// </summary>
    /// <returns>Current location, or null if unavailable.</returns>
    public async Task<LatLng?> GetCurrentLocationAsync()
    {
        try
        {
            var module = await m_moduleTask.Value;
            var result = await module.InvokeAsync<LocationResult?>("getCurrentLocation");
            if (result is null) return null;
            return new LatLng(result.Lat, result.Lng);
        }
        catch (JSException)
        {
            return null;
        }
    }

    /// <summary>
    /// Center map on user's current location and return coordinates.
    /// </summary>
    public async Task<LatLng?> CenterOnCurrentLocationAsync()
    {
        try
        {
            var module = await m_moduleTask.Value;
            var result = await module.InvokeAsync<LocationResult?>("centerOnCurrentLocation");
            if (result is null) return null;
            return new LatLng(result.Lat, result.Lng);
        }
        catch (JSException)
        {
            return null;
        }
    }

    /// <summary>
    /// Pan the map to a specific location.
    /// </summary>
    public async Task PanToAsync(LatLng location)
    {
        var module = await m_moduleTask.Value;
        await module.InvokeVoidAsync("panTo", new { lat = location.Lat, lng = location.Lng });
    }

    /// <summary>
    /// Set the map zoom level.
    /// </summary>
    public async Task SetZoomAsync(int zoom)
    {
        var module = await m_moduleTask.Value;
        await module.InvokeVoidAsync("setZoom", zoom);
    }

    /// <summary>
    /// Dispose of map resources.
    /// </summary>
    public async Task DisposeMapAsync()
    {
        if (m_moduleTask.IsValueCreated)
        {
            var module = await m_moduleTask.Value;
            await module.InvokeVoidAsync("dispose");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (m_disposed) return;
        m_disposed = true;

        if (m_moduleTask.IsValueCreated)
        {
            try
            {
                var module = await m_moduleTask.Value;
                await module.InvokeVoidAsync("dispose");
                await module.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }

    /// <summary>
    /// Internal class for deserializing location results from JavaScript.
    /// </summary>
    private class LocationResult
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}

/// <summary>
/// Marker data for map display.
/// </summary>
public class MapMarker
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Label { get; set; }
    public string? Address { get; set; }
    public string? Icon { get; set; }
    public string? Href { get; set; }

    public MapMarker()
    {
    }

    public MapMarker(LatLng location, string? label = null)
    {
        Lat = location.Lat;
        Lng = location.Lng;
        Label = label;
    }

    public MapMarker(LatLng location, string? label, string? address, string? href)
    {
        Lat = location.Lat;
        Lng = location.Lng;
        Label = label;
        Address = address;
        Href = href;
    }

    /// <summary>
    /// Create a MapMarker from a LatLng with directions URL.
    /// </summary>
    public static MapMarker FromLatLng(LatLng location, string label, string? address = null)
    {
        return new MapMarker
        {
            Lat = location.Lat,
            Lng = location.Lng,
            Label = label,
            Address = address,
            Href = location.GetGoogleMapsUrl()
        };
    }
}
