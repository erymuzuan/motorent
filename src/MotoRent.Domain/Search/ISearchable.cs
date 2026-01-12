namespace MotoRent.Domain.Search;

/// <summary>
/// Interface for entities that can be indexed and searched via OpenSearch.
/// </summary>
public interface ISearchable
{
    /// <summary>
    /// The entity's primary key ID.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// The primary display title for search results.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// The status of the entity for filtering.
    /// </summary>
    string Status { get; }

    /// <summary>
    /// The full searchable text content.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// A brief summary for display in search results.
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// The entity type name for display.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Whether this entity has a date field for date-based indexing.
    /// </summary>
    static virtual bool HasDate => false;

    /// <summary>
    /// Whether this entity is shared across all tenants (uses shared index).
    /// </summary>
    static virtual bool IsShared => false;

    /// <summary>
    /// Whether permission filtering is required for search results.
    /// </summary>
    static virtual bool RequiredPermission => false;

    /// <summary>
    /// Whether dates should be split into separate indices.
    /// </summary>
    bool SplitDate => false;

    /// <summary>
    /// The permission policy ID for filtering.
    /// </summary>
    int? PermissionPolicy => default;

    /// <summary>
    /// Whether to split by year for indexing.
    /// </summary>
    bool SplitYear => false;

    /// <summary>
    /// Whether to split by month for indexing.
    /// </summary>
    bool SplitMonth => false;

    /// <summary>
    /// Optional image store ID for thumbnails.
    /// </summary>
    string? ImageStoreId => default;

    /// <summary>
    /// Optional date for date-based queries.
    /// </summary>
    DateOnly? Date => default;

    /// <summary>
    /// Flag indicating if this entity was populated from a search result.
    /// </summary>
    bool IsSearchResult { get; set; }

    /// <summary>
    /// Custom fields to be indexed for specialized queries.
    /// </summary>
    Dictionary<string, object>? CustomFields => default;
}
