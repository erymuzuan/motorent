namespace MotoRent.Client.Services;

/// <summary>
/// Options for configuring modal dialogs.
/// </summary>
public class ModalOptions
{
    /// <summary>
    /// Modal size: sm, md, lg, xl, or fullscreen.
    /// </summary>
    public ModalSize Size { get; set; } = ModalSize.Large;

    /// <summary>
    /// Whether to show the modal header.
    /// </summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Whether to show a close button in the header.
    /// </summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Whether the modal can be closed by pressing Escape.
    /// </summary>
    public bool CloseOnEsc { get; set; } = true;

    /// <summary>
    /// Whether the modal can be closed by clicking outside.
    /// </summary>
    public bool CloseOnClickOutside { get; set; } = true;

    /// <summary>
    /// Whether to blur the background when the modal is open.
    /// </summary>
    public bool BlurBackground { get; set; }

    /// <summary>
    /// Whether the modal content is scrollable.
    /// </summary>
    public bool Scrollable { get; set; } = true;

    /// <summary>
    /// Whether to center the modal vertically.
    /// </summary>
    public bool Centered { get; set; } = true;

    /// <summary>
    /// Status color for the modal header.
    /// </summary>
    public string? StatusColor { get; set; }
}

/// <summary>
/// Modal size options matching Tabler modal sizes.
/// </summary>
public enum ModalSize
{
    Small,
    Medium,
    Large,
    ExtraLarge,
    Fullscreen
}
