namespace MotoRent.Domain.Entities;

public partial class Rental
{
    public bool IsHistoricalEntry { get; set; }

    public decimal? PaidAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public bool IsPaid { get; set; }

    public string? AgreementStoreId { get; set; }

    public string? NotesHtml { get; set; }

    public List<RentalAttachment> Attachments { get; set; } = [];
}

public class RentalAttachment
{
    public string StoreId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string AttachmentType { get; set; } = "Document";

    public DateTimeOffset UploadedOn { get; set; } = DateTimeOffset.UtcNow;
}
