namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a note/activity log entry for an accident.
/// Used for tracking the timeline of events and communications.
/// </summary>
public class AccidentNote : Entity
{
    public int AccidentNoteId { get; set; }

    /// <summary>
    /// The accident this note belongs to.
    /// </summary>
    public int AccidentId { get; set; }

    #region Note Content

    /// <summary>
    /// Note content/message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of note for categorization.
    /// </summary>
    public string NoteType { get; set; } = "General"; // General, PhoneCall, Email, StatusChange, DocumentAdded, CostAdded

    /// <summary>
    /// Whether this note is pinned/important.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Whether this note is internal only (not shared with external parties).
    /// </summary>
    public bool IsInternal { get; set; } = true;

    #endregion

    #region Activity Log Fields

    /// <summary>
    /// The entity type this activity relates to (Accident, Party, Cost, Document).
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// The ID of the related entity.
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// Previous status (for status changes).
    /// </summary>
    public string? PreviousStatus { get; set; }

    /// <summary>
    /// New status (for status changes).
    /// </summary>
    public string? NewStatus { get; set; }

    #endregion

    public override int GetId() => this.AccidentNoteId;
    public override void SetId(int value) => this.AccidentNoteId = value;
}
