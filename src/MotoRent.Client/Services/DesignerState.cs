using MotoRent.Domain.Models;

namespace MotoRent.Client.Services;

public class DesignerState
{
    public string? DraggingBlockType { get; set; }
    public LayoutBlock? SelectedBlock { get; set; }
    public event Action? OnStateChanged;

    public void SetSelectedBlock(LayoutBlock? block)
    {
        SelectedBlock = block;
        OnStateChanged?.Invoke();
    }

    public void NotifyStateChanged() => OnStateChanged?.Invoke();
}
