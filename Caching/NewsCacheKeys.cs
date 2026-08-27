namespace AutomotiveInfo.Caching;

public static class NewsCacheKeys
{
    /// <summary>
    /// Cache key for the "all articles" list, partitioned by culture so one
    /// language's cached response can never be served to another.
    /// </summary>
    public static string AllArticles(string culture) => $"news:all-articles:{culture.ToLowerInvariant()}";
}
