using AutomotiveInfo.Models;
using AutomotiveInfo.News;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveInfo.Controllers;

/// <summary>
/// Public (website/headless) news endpoint. All querying, caching and culture rules
/// live in <see cref="INewsArticleService"/>; this controller only owns HTTP concerns.
/// </summary>
[ApiController]
[Route("api/v1/news")]
public class NewsApiController : ControllerBase
{
    private readonly INewsArticleService _newsArticleService;

    public NewsApiController(INewsArticleService newsArticleService)
    {
        _newsArticleService = newsArticleService;
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(List<NewsArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetLatest([FromQuery] string? tag, [FromQuery] int count = 3, [FromQuery] string? culture = null)
    {
        count = Math.Clamp(count, 1, 20);

        var resolvedCulture = _newsArticleService.ResolveCulture(culture);
        if (resolvedCulture is null)
        {
            // Unknown culture: reject instead of caching junk under an unbounded, user-supplied key.
            return Problem(
                title: "Unknown culture",
                detail: $"Culture '{culture}' is not configured for this site.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var allArticles = _newsArticleService.GetArticles(resolvedCulture);

        if (allArticles is null)
        {
            // The news list node is missing entirely — a configuration problem,
            // distinct from "the list exists but has no articles" (a valid 200 []).
            return Problem(
                title: "News list not found",
                detail: "No published content of the news list document type exists.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var filtered = allArticles.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(tag))
        {
            filtered = filtered.Where(a =>
                string.Equals(a.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        return Ok(filtered.Take(count).ToList());
    }
}
