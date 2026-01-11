using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MotoRent.Client.Interops;

/// <summary>
/// JavaScript interop for file upload functionality.
/// Handles dropzone initialization, file upload with compression, and EXIF extraction.
/// </summary>
public class FileUploadJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> m_moduleTask;

    public FileUploadJsInterop(IJSRuntime jsRuntime)
    {
        this.m_moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./scripts/file-upload.js").AsTask());
    }

    /// <summary>
    /// Initializes a dropzone for drag-and-drop file upload.
    /// </summary>
    public async Task StartDropZone<T>(ElementReference element, DotNetObjectReference<T> dotNet, object? options = null)
        where T : class
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("startDropZone", element, dotNet, options);
    }

    /// <summary>
    /// Initializes file upload functionality on an input element.
    /// </summary>
    public async Task StartFileUpload<T>(ElementReference element, DotNetObjectReference<T>? dotNet, object? options = null)
        where T : class
    {
        if (dotNet is null) return;

        try
        {
            var module = await this.m_moduleTask.Value;
            await module.InvokeVoidAsync("startFileUpload", element, dotNet, options);
        }
        catch (ObjectDisposedException)
        {
            // Component was disposed before JS could initialize
        }
    }

    /// <summary>
    /// Shows selfie file upload UI on mobile.
    /// </summary>
    public async Task ShowSelfieFileUploadAsync()
    {
        var module = await this.m_moduleTask.Value;
        await module.InvokeVoidAsync("showSelfieFileUpload");
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
