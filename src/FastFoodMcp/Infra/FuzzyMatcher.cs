namespace FastFoodMcp.Infra;

/// <summary>
/// Utility class for calculating string similarity using Levenshtein distance.
/// Used for fuzzy matching and providing suggestions when exact matches fail.
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    public static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;
        
        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distance = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; i++)
            distance[i, 0] = i;
        
        for (var j = 0; j <= targetLength; j++)
            distance[0, j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// Calculates similarity score (0-1, where 1 is identical).
    /// </summary>
    public static double SimilarityScore(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0;

        var maxLength = Math.Max(source.Length, target.Length);
        var distance = LevenshteinDistance(source.ToLowerInvariant(), target.ToLowerInvariant());
        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Finds the top N most similar items from a collection.
    /// </summary>
    public static List<(T Item, double Score)> FindTopMatches<T>(
        string query,
        IEnumerable<T> items,
        Func<T, string> keySelector,
        int topN = 3,
        double minScore = 0.3)
    {
        return items
            .Select(item => (Item: item, Score: SimilarityScore(query, keySelector(item))))
            .Where(x => x.Score >= minScore)
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .ToList();
    }

    /// <summary>
    /// Performs fuzzy search on text fields.
    /// </summary>
    public static bool FuzzyContains(string text, string query, double threshold = 0.6)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            return false;

        // Check for direct substring match first
        if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check similarity with the entire text
        if (SimilarityScore(text, query) >= threshold)
            return true;

        // Check similarity with individual words
        var words = text.Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Any(word => SimilarityScore(word, query) >= threshold);
    }
}
