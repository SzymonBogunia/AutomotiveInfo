using Examine;
using Examine.Search;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;
using static Umbraco.Cms.Core.Constants;

namespace AutomotiveInfo.Search;

/// <summary>
/// A single, presentation-ready search hit. The service owns all content access,
/// so views never dig into <c>IPublishedContent</c> with magic strings.
/// </summary>
public sealed record SearchHit(
    string Title,
    string Url,
    DateTime PublishDate,
    string? TagName,
    string? ImageUrl);

public sealed record ArticleSearchResult(
    IReadOnlyList<SearchHit> Items,
    long TotalResults,
    int TotalPages,
    int PageNumber);

public interface IArticleSearchService
{
    /// <summary>
    /// Finds published articles for the current culture. Either <paramref name="query"/>
    /// or <paramref name="tag"/> (or both) must be supplied — a tag alone browses that tag.
    /// A "#tag" prefix in the query is treated as a tag filter.
    /// </summary>
    ArticleSearchResult Search(string? query, string? tag, int pageNumber, int pageSize);
}

public class ArticleSearchService : IArticleSearchService
{
    private const int MaxPageSize = 50;

    // Relevance weights: a term hit in the title should outrank the same hit in the body,
    // and an article actually *tagged* with the term should rank between the two.
    private const float TitleBoost = 3f;
    private const float TagBoost = 2.5f;
    private const float NodeNameBoost = 2f;

    private static readonly string NewsPageAlias = NewsPage.ModelTypeAlias;
    private static readonly string TagContentAlias = Tag.ModelTypeAlias;

    private readonly IExamineManager _examineManager;
    private readonly IPublishedContentQuery _publishedContentQuery;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IDefaultCultureAccessor _defaultCultureAccessor;
    private readonly ILogger<ArticleSearchService> _logger;

    public ArticleSearchService(
        IExamineManager examineManager,
        IPublishedContentQuery publishedContentQuery,
        IVariationContextAccessor variationContextAccessor,
        IDefaultCultureAccessor defaultCultureAccessor,
        ILogger<ArticleSearchService> logger)
    {
        _examineManager = examineManager;
        _publishedContentQuery = publishedContentQuery;
        _variationContextAccessor = variationContextAccessor;
        _defaultCultureAccessor = defaultCultureAccessor;
        _logger = logger;
    }

    public ArticleSearchResult Search(string? query, string? tag, int pageNumber, int pageSize)
    {
        // Guard the service contract itself, not just the current caller:
        // pageSize = 0 would divide by zero below, negatives would corrupt skip.
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var trimmedQuery = query?.Trim() ?? string.Empty;
        var trimmedTag = tag?.Trim() ?? string.Empty;

        // "#Premiera" typed into the box is shorthand for the tag filter.
        if (trimmedQuery.StartsWith('#'))
        {
            trimmedTag = trimmedQuery.TrimStart('#').Trim();
            trimmedQuery = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(trimmedQuery) && string.IsNullOrWhiteSpace(trimmedTag))
        {
            return Empty(pageNumber);
        }

        if (!_examineManager.TryGetIndex(UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            // Infrastructure failure, not "no results" — make it visible in the logs.
            _logger.LogWarning(
                "Examine index {IndexName} is not available; article search returns no results.",
                UmbracoIndexes.ExternalIndexName);
            return Empty(pageNumber);
        }

        var culture = ResolveCulture();
        var skip = (pageNumber - 1) * pageSize;

        IBooleanOperation? booleanQuery = BuildQuery(index, trimmedQuery, trimmedTag, culture);

        if (booleanQuery is null)
        {
            return Empty(pageNumber);
        }

        var trimmed = string.IsNullOrEmpty(trimmedQuery) ? $"#{trimmedTag}" : trimmedQuery;

        ISearchResults results = booleanQuery.Execute(QueryOptions.SkipTake(skip, pageSize));

        var totalResults = results.TotalItemCount;
        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        var items = results
            .Select(r => int.TryParse(r.Id, out var id) ? _publishedContentQuery.Content(id) : null)
            .OfType<NewsPage>()
            .Select(article => MapToHit(article, culture))
            .ToList();

        _logger.LogDebug(
            "Article search for {Query} (culture {Culture}) returned {TotalHits} hits.",
            trimmed,
            culture,
            totalResults);

        return new ArticleSearchResult(items, totalResults, totalPages, pageNumber);
    }

    private static ArticleSearchResult Empty(int pageNumber) =>
        new(Array.Empty<SearchHit>(), 0, 0, pageNumber);

    /// <summary>
    /// The culture used for the "_{culture}" field suffixes in the index —
    /// taken from the request's variation context (set by the culture domains),
    /// falling back to the site's default culture. Lowercased to match Examine's field naming.
    /// </summary>
    private string ResolveCulture()
    {
        var culture = _variationContextAccessor.VariationContext?.Culture;

        if (string.IsNullOrEmpty(culture))
        {
            culture = _defaultCultureAccessor.DefaultCulture;
        }

        return culture.ToLowerInvariant();
    }

    /// <summary>
    /// Builds one query from an optional text term and an optional tag filter.
    /// The tag (when present) is a hard filter; the text term is a scored match.
    /// Returns null when a requested tag does not exist (so the caller shows "no results"
    /// rather than silently returning everything).
    /// </summary>
    private IBooleanOperation? BuildQuery(IIndex index, string term, string tagName, string culture)
    {
        // newsPage varies by culture, so Examine suffixes its fields per culture
        // (verified in the index: nodeName_pl-pl / nodeName_en, articleTitle_pl-pl / …).
        var titleField = $"articleTitle_{culture}";
        var nodeNameField = $"nodeName_{culture}";
        var bodyField = $"components_{culture}";

        // Only articles actually published in this culture — an article that exists
        // only in Polish must not surface on /en even if an invariant field matches.
        var query = index.Searcher
            .CreateQuery("content")
            .NodeTypeAlias(NewsPageAlias)
            .And()
            .Field($"__Published_{culture}", "y");

        if (!string.IsNullOrWhiteSpace(tagName))
        {
            // The 'tag' field stores MNTP UDIs ("umb://document/{guid:N}"), which the analyzer
            // tokenizes into [umb, document, {guid}]. The first two tokens are shared by every
            // tagged article, but the 32-hex guid token is unique per tag — so matching the guid
            // token alone is exact, single-term, and needs no hand-assembled Lucene syntax.
            var filterTokens = GetTagGuidTokens(index, tagName);
            if (filterTokens.Length == 0)
            {
                return null;
            }

            query = query.And().GroupedOr(new[] { "tag" }, filterTokens);
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            // Tag-only browsing: the filter above is the whole query.
            return query;
        }

        // Unified search: the term is also resolved against tag names, so a visitor
        // typing "premiera" finds articles *tagged* Premiera alongside text matches —
        // no special syntax required.
        var termTagTokens = GetTagGuidTokens(index, term);

        return query.And(nested =>
            {
                // Boosted clauses rank title/name matches above body matches …
                var group = nested
                    .Field(titleField, term.Boost(TitleBoost))
                    .Or()
                    .Field(nodeNameField, term.Boost(NodeNameBoost))
                    .Or()
                    // … while the managed query keeps recall high (multi-word terms,
                    // per-term matching) across all three fields.
                    .ManagedQuery(term, new[] { titleField, nodeNameField, bodyField });

                if (termTagTokens.Length > 0)
                {
                    group = group.Or().GroupedOr(
                        new[] { "tag" },
                        termTagTokens.Select(t => t.Boost(TagBoost)).ToArray());
                }

                return group;
            },
            BooleanOperation.Or);
    }

    /// <summary>
    /// Resolves a tag name to the guid tokens ("N" format, as they appear inside the
    /// indexed "umb://document/{guid:N}" UDI values) of the matching tag nodes.
    /// </summary>
    private static string[] GetTagGuidTokens(IIndex index, string tagName)
    {
        ISearchResults tagNodeResults = index.Searcher
            .CreateQuery("content")
            .NodeTypeAlias(TagContentAlias)
            .And()
            .Field("nodeName", tagName)
            .Execute();

        return tagNodeResults
            .Select(r => r.Values.TryGetValue("__Key", out var key) && Guid.TryParse(key, out var guid)
                ? guid.ToString("N")
                : null)
            .WhereNotNull()
            .Distinct()
            .ToArray();
    }

    private static SearchHit MapToHit(NewsPage article, string culture)
    {
        // Typed model properties resolve through the same variation context the
        // culture was derived from, so title/URL/date stay culture-consistent.
        var title = string.IsNullOrWhiteSpace(article.ArticleTitle)
            ? GetNodeName(article, culture)
            : article.ArticleTitle!;

        var publishDate = article.PublishDate == default ? article.CreateDate : article.PublishDate;

        return new SearchHit(
            Title: title,
            Url: article.Url(culture: culture),
            PublishDate: publishDate,
            TagName: (article.Tag as Tag)?.TagName,
            ImageUrl: article.MainImage?.Url());
    }

    private static string GetNodeName(IPublishedContent article, string culture)
    {
        var match = article.Cultures.FirstOrDefault(kv =>
            string.Equals(kv.Key, culture, StringComparison.OrdinalIgnoreCase));

        return match.Value?.Name ?? article.Name ?? string.Empty;
    }
}
