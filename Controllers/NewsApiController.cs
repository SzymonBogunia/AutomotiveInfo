using AutomotiveInfo.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace AutomotiveInfo.Controllers;

[ApiController]
[Route("api/news")]
public class NewsApiController : Controller
{
    private readonly IPublishedContentQuery _contentQuery;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    // UWAGA: podmień jeśli Twój węzeł-kontener ma inny adres niż "strona-aktualnosci"
    private const string NewsListUrlSegment = "strona-aktualnosci";

    public NewsApiController(
        IPublishedContentQuery contentQuery,
        IUmbracoContextAccessor umbracoContextAccessor)
    {
        _contentQuery = contentQuery;
        _umbracoContextAccessor = umbracoContextAccessor;
    }

    [HttpGet("latest")]
    public IActionResult GetLatest([FromQuery] string? tag, [FromQuery] int count = 3)
    {
        if (count <= 0)
        {
            return BadRequest("count musi być liczbą dodatnią.");
        }

        var newsListPage = _contentQuery
            .ContentAtRoot()
            .SelectMany(root => root.DescendantsOrSelf<IPublishedContent>())
            .FirstOrDefault(x => x.UrlSegment == NewsListUrlSegment);

        if (newsListPage is null)
        {
            return NotFound($"Nie znaleziono węzła listy aktualności o adresie '{NewsListUrlSegment}'.");
        }

        var articles = newsListPage
            .Children()
            .Where(x => x.ContentType.Alias == "newsPage");

        if (!string.IsNullOrWhiteSpace(tag))
        {
            articles = articles.Where(article => ArticleHasTag(article, tag));
        }

        var result = articles
            .OrderByDescending(GetPublishDate)
            .Take(count)
            .Select(MapToDto)
            .ToList();

        return Ok(result);
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