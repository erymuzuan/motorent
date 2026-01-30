namespace MotoRent.Client.Pages.Bookings;

/// <summary>
/// Result DTO returned from the scan document dialog to populate booking fields.
/// </summary>
public class ScanDocumentResult
{
    public string? CustomerName { get; set; }
    public string? PassportNo { get; set; }
    public string? Nationality { get; set; }
}
