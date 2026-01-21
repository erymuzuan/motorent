namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Represents a 2D screen position for overlay markers.
/// Used to position marker icons on the canvas overlay.
/// </summary>
public class ScreenPosition
{
    /// <summary>
    /// X coordinate in pixels from the left edge.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate in pixels from the top edge.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Creates an empty screen position.
    /// </summary>
    public ScreenPosition() { }

    /// <summary>
    /// Creates a screen position with coordinates.
    /// </summary>
    public ScreenPosition(double x, double y)
    {
        this.X = x;
        this.Y = y;
    }
}
