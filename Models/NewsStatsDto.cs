namespace AutomotiveInfo.Models;

public class NewsTagCountDto
{
    /// <summary>Tag name; empty string for untagged articles (label is a UI concern).</summary>
    public string Tag { get; set; } = string.Empty;

    public int Count { get; set; }
}

/// <summary>
/// Editorial statistics computed server-side over the complete article list —
/// clients must not aggregate from a paged/capped endpoint (that's how the
/// dashboard ended up silently reporting numbers based on 20 of N articles).
/// </summary>
public class NewsStatsDto
{
    public int TotalArticles { get; set; }

    public List<NewsTagCountDto> TagCounts { get; set; } = new();

    public List<NewsArticleDto> RecentArticles { get; set; } = new();
}
