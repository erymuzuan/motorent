using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Lookups;

/// <summary>
/// Result from Gemini vehicle image recognition.
/// Contains both raw recognized data and matched lookup entries.
/// </summary>
public class VehicleRecognitionResult
{
    /// <summary>
    /// Confidence score from Gemini (0.0 - 1.0).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Recognized vehicle type.
    /// </summary>
    public VehicleType? VehicleType { get; set; }

    /// <summary>
    /// Recognized brand/make name (as extracted by Gemini).
    /// </summary>
    public string? RecognizedMake { get; set; }

    /// <summary>
    /// Recognized model name (as extracted by Gemini).
    /// </summary>
    public string? RecognizedModel { get; set; }

    /// <summary>
    /// Matched VehicleModel from lookup database (null if no match found).
    /// </summary>
    public VehicleModel? MatchedVehicleModel { get; set; }

    /// <summary>
    /// Extracted license plate (Thai format, e.g., "กก 1234").
    /// </summary>
    public string? LicensePlate { get; set; }

    /// <summary>
    /// Recognized vehicle color.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Estimated year of manufacture.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Recognized car segment (for cars only).
    /// </summary>
    public CarSegment? Segment { get; set; }

    /// <summary>
    /// Recognized engine CC (for motorbikes).
    /// </summary>
    public int? EngineCC { get; set; }

    /// <summary>
    /// Raw JSON response from Gemini for debugging.
    /// </summary>
    public string? RawJson { get; set; }

    /// <summary>
    /// Error message if recognition failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Whether recognition was successful (no errors).
    /// </summary>
    public bool IsSuccess => string.IsNullOrEmpty(Error);

    /// <summary>
    /// Whether a lookup database match was found.
    /// </summary>
    public bool HasLookupMatch => MatchedVehicleModel != null;

    /// <summary>
    /// Whether this vehicle should be suggested for addition to lookups.
    /// True when Gemini recognized it with high confidence but no match exists.
    /// </summary>
    public bool SuggestAddToLookups => !HasLookupMatch &&
        !string.IsNullOrEmpty(RecognizedMake) &&
        !string.IsNullOrEmpty(RecognizedModel) &&
        Confidence > 0.7m;
}
