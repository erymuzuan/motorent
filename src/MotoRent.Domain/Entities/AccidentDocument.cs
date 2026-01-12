namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a document/file attached to an accident.
/// </summary>
public class AccidentDocument : Entity
{
    public int AccidentDocumentId { get; set; }

    /// <summary>
    /// The accident this document belongs to.
    /// </summary>
    public int AccidentId { get; set; }

    /// <summary>
    /// Type of document.
    /// </summary>
    public AccidentDocumentType DocumentType { get; set; }

    #region File Information

    /// <summary>
    /// Original filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Storage path or URL.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the file.
    /// </summary>
    public string? ContentType { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// Document title/description.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Additional notes about this document.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date shown on the document (if different from upload date).
    /// </summary>
    public DateTimeOffset? DocumentDate { get; set; }

    /// <summary>
    /// When the document was uploaded.
    /// </summary>
    public DateTimeOffset UploadedDate { get; set; }

    /// <summary>
    /// Who uploaded the document.
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the party this document relates to (optional).
    /// </summary>
    public int? AccidentPartyId { get; set; }

    #endregion

    public override int GetId() => this.AccidentDocumentId;
    public override void SetId(int value) => this.AccidentDocumentId = value;
}
