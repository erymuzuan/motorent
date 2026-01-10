namespace MotoRent.Client.Services;

/// <summary>
/// Represents a modal instance that can be used by dialog components
/// to close themselves and return results.
/// </summary>
public class ModalInstance
{
    private readonly IModalService m_modalService;

    public ModalInstance(IModalService modalService)
    {
        this.m_modalService = modalService;
    }

    /// <summary>
    /// Closes the modal with a successful result and optional data.
    /// </summary>
    public void Close(object? data = null)
    {
        this.m_modalService.Close(ModalResult.Ok(data));
    }

    /// <summary>
    /// Cancels the modal without returning any data.
    /// </summary>
    public void Cancel()
    {
        this.m_modalService.Close(ModalResult.Cancel());
    }
}
