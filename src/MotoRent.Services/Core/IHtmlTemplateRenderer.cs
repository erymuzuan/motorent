using MotoRent.Domain.Models;

namespace MotoRent.Services.Core;

/// <summary>
/// Service to generate HTML documents based on a DocumentLayout and token data.
/// </summary>
public interface IHtmlTemplateRenderer
{
    /// <summary>
    /// Generates HTML string from a layout and data.
    /// </summary>
    string RenderHtml(DocumentLayout layout, Dictionary<string, object?> data);
}
