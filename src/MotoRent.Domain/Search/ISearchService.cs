using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Search;

/// <summary>
/// Operator for combining search terms.
/// </summary>
public enum ChainAndOr
{
    None,
    And,
    Or
}

/// <summary>
/// Options for configuring search behavior.
/// </summary>
/// <param name="Field">The specific field to search in.</param>
/// <param name="Fuzziness">Fuzziness setting for fuzzy matching (e.g., "AUTO", "1", "2").</param>
/// <param name="Operator">How to combine search terms.</param>
/// <param name="SortAsc">Field name to sort ascending.</param>
/// <param name="SortDesc">Field name to sort descending.</param>
public record SearchOptions(
    string? Field = null,
    string? Fuzziness = null,
    ChainAndOr Operator = ChainAndOr.And,
    string? SortAsc = null,
    string? SortDesc = null);

/// <summary>
/// Service interface for full-text search operations using OpenSearch.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Performs fuzzy search on an in-memory dictionary of items.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="items">Dictionary of ID to searchable text.</param>
    /// <param name="maxEditDistance">Maximum Levenshtein distance for word matching.</param>
    /// <param name="minScoreThreshold">Minimum score (0.0-1.0) to include in results.</param>
    /// <returns>List of matching results sorted by score.</returns>
    List<FuzzySearchResult> FuzzySearch(
        string query,
        IDictionary<string, string> items,
        int maxEditDistance = 2,
        double minScoreThreshold = 0.4);

    /// <summary>
    /// Searches the index and returns fully hydrated entities from the database.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ISearchable.</typeparam>
    /// <param name="query">The search query text.</param>
    /// <param name="skip">Number of results to skip.</param>
    /// <param name="take">Maximum number of results to return.</param>
    /// <returns>Array of entities retrieved from database.</returns>
    ValueTask<T[]> SearchEntityAsync<T>(string query, int skip = 0, int take = 20)
        where T : Entity, ISearchable, new();

    /// <summary>
    /// Searches the index and returns lightweight search results.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ISearchable.</typeparam>
    /// <param name="query">The search query text.</param>
    /// <param name="options">Optional search configuration.</param>
    /// <param name="skip">Number of results to skip.</param>
    /// <param name="take">Maximum number of results to return.</param>
    /// <returns>Array of search results.</returns>
    ValueTask<SearchResult[]> SearchAsync<T>(string query, SearchOptions? options = null, int skip = 0, int take = 20)
        where T : Entity, ISearchable, new();

    /// <summary>
    /// Searches multiple entity types at once.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="types">Array of entity type names to search.</param>
    /// <param name="skip">Number of results to skip.</param>
    /// <param name="take">Maximum number of results to return.</param>
    /// <returns>Array of search results from all types.</returns>
    Task<SearchResult[]> SearchAsync(string query, string[] types, int skip = 0, int take = 20);

    /// <summary>
    /// Searches the index and returns a paged load operation.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">The search query text.</param>
    /// <param name="skip">Number of results to skip.</param>
    /// <param name="take">Maximum number of results to return.</param>
    /// <returns>LoadOperation with paging info.</returns>
    ValueTask<LoadOperation<T>> PagedSearchAsync<T>(string query, int skip = 0, int take = 20)
        where T : Entity, new();

    /// <summary>
    /// Gets distinct values for a specific field.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ISearchable.</typeparam>
    /// <typeparam name="TResult">The type of field values.</typeparam>
    /// <param name="field">The field name to aggregate.</param>
    /// <returns>Array of distinct field values.</returns>
    ValueTask<TResult[]> GetDistinctFieldValues<T, TResult>(string field)
        where T : Entity, ISearchable, new() => new([]);

    /// <summary>
    /// Deletes documents from the index matching a field value.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <param name="selector">Function to select the field.</param>
    /// <param name="value">Value to match for deletion.</param>
    Task DeleteAsync<T, TField>(Func<T, TField> selector, TField value);

    /// <summary>
    /// Searches for code items (lookup codes).
    /// </summary>
    /// <param name="type">The code type/category.</param>
    /// <param name="term">Search term.</param>
    /// <returns>Array of matching code items.</returns>
    Task<CodeItem[]> SearchCodeItemsAsync(string type, string term);
}
