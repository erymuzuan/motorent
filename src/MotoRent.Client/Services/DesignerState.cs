using MotoRent.Domain.Models;

namespace MotoRent.Client.Services;

public class DesignerState
{
    public string? DraggingBlockType { get; set; }
    public LayoutBlock? SelectedBlock { get; set; }
    public event Action? OnStateChanged;

    public void SetSelectedBlock(LayoutBlock? block)
    {
        this.SelectedBlock = block;
        this.OnStateChanged?.Invoke();
    }

    public void NotifyStateChanged() => this.OnStateChanged?.Invoke();
}
