using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;

namespace MotoRent.Client.Services;

public class DesignerState
{
    public string? DraggingBlockType { get; set; }
    public LayoutBlock? SelectedBlock { get; set; }
    public DocumentType DocumentType { get; set; }
    public int LeftPanelWidth { get; set; } = 220;
    public int RightPanelWidth { get; set; } = 280;
    public bool LeftCollapsed { get; set; }
    public bool RightCollapsed { get; set; }
    public int CurrentPageIndex { get; set; }
    public string ActiveRightTab { get; set; } = "properties";
    public string? ActiveBlockId { get; set; }
    public event Action? OnStateChanged;

    public void SetSelectedBlock(LayoutBlock? block)
    {
        this.SelectedBlock = block;
        this.OnStateChanged?.Invoke();
    }

    public void SetCurrentPage(int index)
    {
        this.CurrentPageIndex = index;
        this.SelectedBlock = null;
        this.OnStateChanged?.Invoke();
    }

    public void SetRightTab(string tab)
    {
        this.ActiveRightTab = tab;
        this.OnStateChanged?.Invoke();
    }

    public void ToggleLeftPanel()
    {
        this.LeftCollapsed = !this.LeftCollapsed;
        this.OnStateChanged?.Invoke();
    }

    public void ToggleRightPanel()
    {
        this.RightCollapsed = !this.RightCollapsed;
        this.OnStateChanged?.Invoke();
    }

    public void NotifyStateChanged() => this.OnStateChanged?.Invoke();
}
