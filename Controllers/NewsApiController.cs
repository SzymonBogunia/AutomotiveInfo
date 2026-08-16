using AutomotiveInfo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using AutomotiveInfo.Caching;

namespace AutomotiveInfo.Controllers;

[ApiController]
[Route("api/news")]
public class NewsApiController : Controller
{
    private readonly IPublishedContentQuery _contentQuery;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IMemoryCache _cache;

    private const string NewsListUrlSegment = "strona-aktualnosci";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public NewsApiController(
        IPublishedContentQuery contentQuery,
        IUmbracoContextAccessor umbracoContextAccessor,
        IMemoryCache cache)
    {
        _contentQuery = contentQuery;
        _umbracoContextAccessor = umbracoContextAccessor;
        _cache = cache;
    }

    [HttpGet("latest")]
    public IActionResult GetLatest([FromQuery] string? tag, [FromQuery] int count = 3)
    {
        if (count <= 0)
        {
            return BadRequest("count musi być liczbą dodatnią.");
        }

        var allArticles = _cache.GetOrCreate(NewsCacheKeys.AllArticles, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return LoadAllArticlesFromContent();
        }) ?? new List<NewsArticleDto>();

        var filtered = allArticles.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(tag))
        {
            filtered = filtered.Where(a =>
                string.Equals(a.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        var result = filtered.Take(count).ToList();

        return Ok(result);
    }

    private List<NewsArticleDto> LoadAllArticlesFromContent()
    {
        var newsListPage = _contentQuery
            .ContentAtRoot()
            .SelectMany(root => root.DescendantsOrSelf<IPublishedContent>())
            .FirstOrDefault(x => x.UrlSegment == NewsListUrlSegment);

        if (newsListPage is null)
        {
            return new List<NewsArticleDto>();
        }

        return newsListPage
            .Children()
            .Where(x => x.ContentType.Alias == "newsPage")
            .OrderByDescending(GetPublishDate)
            .Select(MapToDto)
            .ToList();
    }

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

    private static bool ArticleHasTag(IPublishedContent article, string tag)
    {
        var tagItems = GetPickerItems(article, "tag");
        return tagItems.Any(t =>
            string.Equals(t.Value<string>("tagName"), tag, StringComparison.OrdinalIgnoreCase));
    }

    private static DateTime GetPublishDate(IPublishedContent article)
    {
        var publishDate = article.Value<DateTime?>("publishDate");
        return publishDate ?? article.CreateDate;
    }

    private static NewsArticleDto MapToDto(IPublishedContent article)
    {
        var firstTag = GetPickerItems(article, "tag").FirstOrDefault()?.Value<string>("tagName");
        var image = GetPickerItems(article, "mainImage").FirstOrDefault();

        return new NewsArticleDto
        {
            Title = article.Value<string>("articleTitle") ?? article.Name ?? string.Empty,
            Url = article.Url(),
            Date = GetPublishDate(article),
            Tag = firstTag,
            ImageUrl = image?.Url()
        };
    }
}