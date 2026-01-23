namespace MotoRent.Domain.Models.VehicleInspection;

/// <summary>
/// Represents the camera state for the 3D model viewer.
/// Used to restore the view to a specific angle/zoom when loading an inspection.
/// </summary>
public class CameraState
{
    /// <summary>
    /// Camera orbit angle in degrees (horizontal rotation around Y-axis).
    /// </summary>
    public double OrbitTheta { get; set; }

    /// <summary>
    /// Camera orbit angle in degrees (vertical tilt).
    /// </summary>
    public double OrbitPhi { get; set; }

    /// <summary>
    /// Camera orbit radius (distance from center).
    /// </summary>
    public double OrbitRadius { get; set; }

    /// <summary>
    /// Camera target X coordinate.
    /// </summary>
    public double TargetX { get; set; }

    /// <summary>
    /// Camera target Y coordinate.
    /// </summary>
    public double TargetY { get; set; }

    /// <summary>
    /// Camera target Z coordinate.
    /// </summary>
    public double TargetZ { get; set; }

    /// <summary>
    /// Field of view in degrees.
    /// </summary>
    public double FieldOfView { get; set; } = 45;

    /// <summary>
    /// Creates an empty camera state with default values.
    /// </summary>
    public CameraState() { }

    /// <summary>
    /// Creates a camera state with orbit parameters.
    /// </summary>
    public CameraState(double theta, double phi, double radius)
    {
        this.OrbitTheta = theta;
        this.OrbitPhi = phi;
        this.OrbitRadius = radius;
    }
}
