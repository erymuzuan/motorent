namespace MotoRent.Worker.Infrastructure;

/// <summary>
/// Metadata about a discovered subscriber.
/// </summary>
public class SubscriberMetadata
{
    /// <summary>
    /// Assembly containing the subscriber.
    /// </summary>
    public string? Assembly { get; set; }

    /// <summary>
    /// Full type name of the subscriber.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Type of the subscriber.
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Simple name of the subscriber.
    /// </summary>
    public string? Name { get; set; }
}
