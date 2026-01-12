using MotoRent.Domain.Search;

namespace MotoRent.Services.Search;

public partial class OpenSearchService
{
    /// <summary>
    /// Performs in-memory fuzzy search using Levenshtein distance.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="items">Dictionary of ID to searchable text.</param>
    /// <param name="maxEditDistance">Maximum allowed edit distance for matching.</param>
    /// <param name="minScoreThreshold">Minimum score (0.0-1.0) to include in results.</param>
    /// <returns>List of matching results sorted by score.</returns>
    public List<FuzzySearchResult> FuzzySearch(
        string query,
        IDictionary<string, string> items,
        int maxEditDistance = 2,
        double minScoreThreshold = 0.4)
    {
        var results = new List<FuzzySearchResult>();

        // Normalize and split the query into unique words
        var queryWords = query.ToLower()
            .Split([' '], StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();

        if (queryWords.Count == 0)
            return results;

        foreach (var item in items)
        {
            // Normalize and split the item into unique words
            var itemWords = item.Value.ToLower()
                .Split([' '], StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            if (itemWords.Count == 0)
                continue;

            var matchCount = 0;

            // Compare each query word to each item word
            foreach (var queryWord in queryWords)
            {
                var wordFound = false;
                foreach (var itemWord in itemWords)
                {
                    var distance = GetLevenshteinDistance(queryWord, itemWord);
                    if (distance <= maxEditDistance)
                    {
                        wordFound = true;
                        break;
                    }
                }

                if (wordFound)
                {
                    matchCount++;
                }
            }

            // Calculate score based on matched words
            var score = (double)matchCount / queryWords.Count;

            if (score >= minScoreThreshold)
            {
                results.Add(new FuzzySearchResult
                {
                    Id = item.Key,
                    Item = item.Value,
                    Score = score
                });
            }
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    private static int GetLevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; d[i, 0] = i++) { }
        for (var j = 0; j <= m; d[0, j] = j++) { }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
