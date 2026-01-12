namespace MotoRent.Domain.Search;

/// <summary>
/// Represents a search result from OpenSearch.
/// </summary>
/// <param name="Id">The entity's primary key.</param>
/// <param name="Type">The entity type display name.</param>
/// <param name="Title">The primary display title.</param>
/// <param name="Summary">A brief summary.</param>
/// <param name="Text">The full searchable text.</param>
public record SearchResult(int Id, string Type, string Title, string Summary, string Text)
{
    /// <summary>
    /// The concrete entity type name (e.g., "Renter", "Vehicle").
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// The date associated with this entity, if any.
    /// </summary>
    public DateOnly? Date { get; set; }

    /// <summary>
    /// Custom fields stored in the index.
    /// </summary>
    public Dictionary<string, object> Item { get; set; } = new();

    /// <summary>
    /// Image store ID for thumbnails.
    /// </summary>
    public string? ImageStoreId { get; set; }

    /// <summary>
    /// The relevance score from the search engine.
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// The entity status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Permission policy ID for access control.
    /// </summary>
    public int? PermissionPolicy { get; set; }

    /// <summary>
    /// Total number of hits for the query (useful for pagination).
    /// </summary>
    public int? TotalHits { get; set; }

    /// <summary>
    /// Gets a typed value from the Item dictionary.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value cast to T, or default if not found.</returns>
    public T? GetValue<T>(string key)
    {
        if (!Item.TryGetValue(key, out var value))
            return default;

        if (value is T typed)
            return typed;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}

/// <summary>
/// Represents a fuzzy search result with matching score.
/// </summary>
public class FuzzySearchResult
{
    /// <summary>
    /// The original item identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The original item text that matched the query.
    /// </summary>
    public string Item { get; set; } = string.Empty;

    /// <summary>
    /// A score from 0.0 to 1.0 indicating match quality.
    /// 1.0 is a perfect match.
    /// </summary>
    public double Score { get; set; }

    public override string ToString() => $"[Score: {Score:N2}] {Item}";
}

/// <summary>
/// Represents a code lookup item (e.g., country codes, currency codes).
/// </summary>
/// <param name="Type">The code category/type.</param>
/// <param name="Code">The code value.</param>
/// <param name="Description">Human-readable description.</param>
public record CodeItem(string Type, string Code, string Description)
{
    /// <summary>
    /// Additional data associated with this code item.
    /// </summary>
    public Dictionary<string, object>? Bags { get; set; }

    /// <summary>
    /// Display string for UI.
    /// </summary>
    public string Display => Description;
}
