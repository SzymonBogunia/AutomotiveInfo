using AutomotiveInfo.Caching;
using AutomotiveInfo.Models;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace AutomotiveInfo.News;

public interface INewsArticleService
{
    /// <summary>
    /// Maps the requested culture onto one actually configured on the site (bounded set),
    /// falling back to the ambient variation context, then the default culture.
    /// Returns null when an explicitly requested culture does not exist.
    /// </summary>
    string? ResolveCulture(string? requested);

    /// <summary>
    /// All published articles for the culture, newest first (cached per culture).
    /// Returns null when the news list node itself is missing (a configuration problem),
    /// as opposed to an empty list (the node exists but holds no published articles).
    /// </summary>
    IReadOnlyList<NewsArticleDto>? GetArticles(string culture);

    /// <summary>
    /// Tags in use by published articles, with counts — derived from the cached article
    /// list, so building the search facet costs no extra queries.
    /// </summary>
    IReadOnlyList<TagFacet> GetTagFacets(string culture);
}

/// <summary>
/// Single owner of the "news articles" query and its per-culture cache — shared by the
/// public news API and the backoffice statistics API so the caching, culture and mapping
/// rules cannot drift between consumers.
/// </summary>
public class NewsArticleService : INewsArticleService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IPublishedContentQuery _contentQuery;
    private readonly IMemoryCache _cache;
    private readonly NewsCacheSignal _cacheSignal;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IDefaultCultureAccessor _defaultCultureAccessor;

    public NewsArticleService(
        IPublishedContentQuery contentQuery,
        IMemoryCache cache,
        NewsCacheSignal cacheSignal,
        IVariationContextAccessor variationContextAccessor,
        IDefaultCultureAccessor defaultCultureAccessor)
    {
        _contentQuery = contentQuery;
        _cache = cache;
        _cacheSignal = cacheSignal;
        _variationContextAccessor = variationContextAccessor;
        _defaultCultureAccessor = defaultCultureAccessor;
    }

    public string? ResolveCulture(string? requested)
    {
        var siteCultures = _contentQuery
            .ContentAtRoot()
            .SelectMany(x => x.Cultures.Keys)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!string.IsNullOrWhiteSpace(requested))
        {
            return siteCultures.FirstOrDefault(c =>
                string.Equals(c, requested.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var ambient = _variationContextAccessor.VariationContext?.Culture;
        if (!string.IsNullOrEmpty(ambient) &&
            siteCultures.Any(c => string.Equals(c, ambient, StringComparison.OrdinalIgnoreCase)))
        {
            return ambient;
        }

        return _defaultCultureAccessor.DefaultCulture;
    }

    public IReadOnlyList<NewsArticleDto>? GetArticles(string culture) =>
        _cache.GetOrCreate(NewsCacheKeys.AllArticles(culture), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            // One shared signal expires every per-culture entry when an article is published.
            entry.AddExpirationToken(_cacheSignal.CreateChangeToken());
            return LoadAllArticlesFromContent(culture);
        });

    public IReadOnlyList<TagFacet> GetTagFacets(string culture) =>
        (GetArticles(culture) ?? Array.Empty<NewsArticleDto>())
            .Where(a => !string.IsNullOrWhiteSpace(a.Tag))
            .GroupBy(a => a.Tag!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TagFacet(g.Key, g.Count()))
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.Name)
            .ToList();

    private List<NewsArticleDto>? LoadAllArticlesFromContent(string culture)
    {
        // Typed lookup by document type: culture-independent and short-circuits
        // on the first match instead of materialising the whole tree.
        var newsListPage = _contentQuery
            .ContentAtRoot()
            .SelectMany(root => root.DescendantsOrSelf<NewsListPage>())
            .FirstOrDefault();

        if (newsListPage is null)
        {
            // Missing container is a configuration problem — distinct from "no articles yet".
            return null;
        }

        return newsListPage
            .Children()
            .Where(x => x.ContentType.Alias == NewsPage.ModelTypeAlias)
            // Only articles actually published in the requested culture —
            // otherwise the list would leak untranslated items with broken URLs.
            .Where(x => IsPublishedInCulture(x, culture))
            .OrderByDescending(GetPublishDate)
            .Select(article => MapToDto(article, culture))
            .ToList();
    }

    private static bool IsPublishedInCulture(IPublishedContent content, string culture) =>
        content.Cultures.Keys.Any(k => string.Equals(k, culture, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<IPublishedContent> GetPickerItems(IPublishedContent content, string alias)
    {
        var raw = content.Value(alias);
        return raw switch
        {
            IEnumerable<IPublishedContent> multiple => multiple,
            IPublishedContent single => new[] { single },
            _ => Enumerable.Empty<IPublishedContent>()
        };
    }

    private static DateTime GetPublishDate(IPublishedContent article)
    {
        // publishDate is invariant (does not vary by culture), so no culture is passed here.
        var publishDate = article.Value<DateTime?>("publishDate");
        return publishDate ?? article.CreateDate;
    }

    private static NewsArticleDto MapToDto(IPublishedContent article, string culture)
    {
        // tag / mainImage are invariant pickers; tagName lives on the invariant 'tag' type.
        var firstTag = GetPickerItems(article, "tag").FirstOrDefault()?.Value<string>("tagName");
        var image = GetPickerItems(article, "mainImage").FirstOrDefault();

        var title = article.Value<string>("articleTitle", culture: culture);

        return new NewsArticleDto
        {
            Title = string.IsNullOrWhiteSpace(title) ? GetNodeName(article, culture) : title,
            Url = article.Url(culture: culture),
            Date = GetPublishDate(article),
            Tag = firstTag,
            // Use the 'card' crop defined on the media picker data type (16:9, focal-point
            // aware) instead of serving the full-size original into card slots.
            ImageUrl = image is MediaWithCrops imageWithCrops
                ? imageWithCrops.GetCropUrl("card")
                : image?.Url()
        };
    }

    private static string GetNodeName(IPublishedContent article, string culture)
    {
        var match = article.Cultures.FirstOrDefault(kv =>
            string.Equals(kv.Key, culture, StringComparison.OrdinalIgnoreCase));

        return match.Value?.Name ?? article.Name ?? string.Empty;
    }
}
