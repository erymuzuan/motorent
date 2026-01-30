using MotoRent.Domain.Models;

namespace MotoRent.Client.Services;

public class DesignerContextMenuEventArgs
{
    public double ClientX { get; set; }
    public double ClientY { get; set; }
    public string BlockId { get; set; } = string.Empty;
    public LayoutBlock? Block { get; set; }
}
