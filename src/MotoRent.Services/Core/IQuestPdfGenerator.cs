using MotoRent.Domain.Models;

namespace MotoRent.Services.Core;

/// <summary>
/// Service to generate PDF documents using QuestPDF based on a DocumentLayout and token data.
/// </summary>
public interface IQuestPdfGenerator
{
    /// <summary>
    /// Generates a PDF byte array from a layout and data.
    /// </summary>
    byte[] GeneratePdf(DocumentLayout layout, Dictionary<string, object?> data);
}
