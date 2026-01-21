using System.Text.Json.Serialization;

namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Represents a 3D position on a vehicle model for damage marker placement.
/// Uses normalized coordinates relative to the model's bounding box.
/// </summary>
public class MarkerPosition
{
    /// <summary>
    /// X coordinate in 3D space (left/right). Normalized to model scale.
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>
    /// Y coordinate in 3D space (up/down). Normalized to model scale.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Z coordinate in 3D space (front/back). Normalized to model scale.
    /// </summary>
    [JsonPropertyName("z")]
    public double Z { get; set; }

    /// <summary>
    /// Surface normal X component for orienting the marker icon.
    /// </summary>
    [JsonPropertyName("normalX")]
    public double NormalX { get; set; }

    /// <summary>
    /// Surface normal Y component for orienting the marker icon.
    /// </summary>
    [JsonPropertyName("normalY")]
    public double NormalY { get; set; }

    /// <summary>
    /// Surface normal Z component for orienting the marker icon.
    /// </summary>
    [JsonPropertyName("normalZ")]
    public double NormalZ { get; set; }

    /// <summary>
    /// Screen X coordinate when marker was created (for rendering).
    /// </summary>
    [JsonPropertyName("screenX")]
    public double? ScreenX { get; set; }

    /// <summary>
    /// Screen Y coordinate when marker was created (for rendering).
    /// </summary>
    [JsonPropertyName("screenY")]
    public double? ScreenY { get; set; }

    /// <summary>
    /// Creates an empty marker position.
    /// </summary>
    public MarkerPosition() { }

    /// <summary>
    /// Creates a marker position with coordinates.
    /// </summary>
    public MarkerPosition(double x, double y, double z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    /// <summary>
    /// Creates a marker position with coordinates and normal.
    /// </summary>
    public MarkerPosition(double x, double y, double z, double normalX, double normalY, double normalZ)
        : this(x, y, z)
    {
        this.NormalX = normalX;
        this.NormalY = normalY;
        this.NormalZ = normalZ;
    }
}
