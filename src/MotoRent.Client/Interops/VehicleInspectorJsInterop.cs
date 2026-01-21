using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MotoRent.Domain.Models.VehicleInspection;

namespace MotoRent.Client.Interops;

/// <summary>
/// JavaScript interop for the 3D vehicle inspector component.
/// Handles model-viewer integration and canvas overlay for damage markers.
/// </summary>
public class VehicleInspectorJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> m_moduleTask;

    public VehicleInspectorJsInterop(IJSRuntime jsRuntime)
    {
        this.m_moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./scripts/vehicle-inspector.js").AsTask());
    }

    /// <summary>
    /// Initializes the vehicle inspector with model-viewer and canvas elements.
    /// </summary>
    public async Task<InitializeResult> InitializeAsync<T>(
        ElementReference modelViewer,
        ElementReference canvas,
        DotNetObjectReference<T> dotNet,
        InspectorOptions? options = null) where T : class
    {
        var module = await this.m_moduleTask.Value;
        return await module.InvokeAsync<InitializeResult>("initialize", modelViewer, canvas, dotNet, options);
    }

    /// <summary>
    /// Updates the markers on the canvas overlay.
    /// </summary>
    public async Task UpdateMarkersAsync(IEnumerable<DamageMarkerDto> markers)
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("updateMarkers", markers);
    }

    /// <summary>
    /// Gets the current camera state for persistence.
    /// </summary>
    public async Task<CameraState?> GetCameraStateAsync()
    {
        var module = await this.m_moduleTask.Value;
        return await module.InvokeAsync<CameraState?>("getCameraState");
    }

    /// <summary>
    /// Restores a saved camera state.
    /// </summary>
    public async Task SetCameraStateAsync(CameraState state)
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("setCameraState", state);
    }

    /// <summary>
    /// Loads a 3D model into the viewer.
    /// </summary>
    public async Task<LoadModelResult> LoadModelAsync(string modelPath)
    {
        var module = await this.m_moduleTask.Value;
        return await module.InvokeAsync<LoadModelResult>("loadModel", modelPath);
    }

    /// <summary>
    /// Takes a snapshot of the current view.
    /// </summary>
    public async Task<string?> TakeSnapshotAsync()
    {
        var module = await this.m_moduleTask.Value;
        return await module.InvokeAsync<string?>("takeSnapshot");
    }

    /// <summary>
    /// Highlights a specific marker by index.
    /// </summary>
    public async Task HighlightMarkerAsync(int index)
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("highlightMarker", index);
    }

    /// <summary>
    /// Clears the marker highlight.
    /// </summary>
    public async Task ClearHighlightAsync()
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("clearHighlight");
    }

    /// <summary>
    /// Focuses the camera on a specific 3D position.
    /// </summary>
    public async Task FocusOnPositionAsync(MarkerPosition position)
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("focusOnPosition", position);
    }

    /// <summary>
    /// Resets the camera to the default view.
    /// </summary>
    public async Task ResetCameraAsync()
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("resetCamera");
    }

    /// <summary>
    /// Enables or disables camera interaction.
    /// </summary>
    public async Task SetInteractionEnabledAsync(bool enabled)
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("setInteractionEnabled", enabled);
    }

    /// <summary>
    /// Converts a 3D position to screen coordinates.
    /// </summary>
    public async Task<ScreenPosition?> PositionToScreenAsync(MarkerPosition position)
    {
        var module = await this.m_moduleTask.Value;
        return await module.InvokeAsync<ScreenPosition?>("positionToScreen", position);
    }

    /// <summary>
    /// Gets updated screen positions for all markers.
    /// </summary>
    public async Task<List<ScreenPositionUpdate>> GetUpdatedScreenPositionsAsync()
    {
        var module = await this.m_moduleTask.Value;
        return await module.InvokeAsync<List<ScreenPositionUpdate>>("getUpdatedScreenPositions");
    }

    /// <summary>
    /// Disposes of the JS module resources.
    /// </summary>
    public async Task DisposeInspectorAsync()
    {
        if (this.m_moduleTask.IsValueCreated)
        {
            var module = await this.m_moduleTask.Value;
            await module.InvokeVoidAsync("dispose");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this.m_moduleTask.IsValueCreated)
        {
            var module = await this.m_moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}

/// <summary>
/// Result of the initialize operation.
/// </summary>
public class InitializeResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of the load model operation.
/// </summary>
public class LoadModelResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Options for initializing the inspector.
/// </summary>
public class InspectorOptions
{
    public List<DamageMarkerDto>? Markers { get; set; }
}

/// <summary>
/// DTO for transferring marker data to JavaScript.
/// </summary>
public class DamageMarkerDto
{
    public Guid MarkerId { get; set; }
    public MarkerPositionDto Position { get; set; } = new();
    public string DamageType { get; set; } = "Scratch";
    public string Severity { get; set; } = "Minor";
    public bool IsPreExisting { get; set; }
}

/// <summary>
/// DTO for marker 3D position.
/// </summary>
public class MarkerPositionDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double NormalX { get; set; }
    public double NormalY { get; set; }
    public double NormalZ { get; set; }
    public double? ScreenX { get; set; }
    public double? ScreenY { get; set; }
}

/// <summary>
/// Update for a marker's screen position.
/// </summary>
public class ScreenPositionUpdate
{
    public Guid MarkerId { get; set; }
    public ScreenPosition? ScreenPosition { get; set; }
}
