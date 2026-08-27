using Asp.Versioning;
using AutomotiveInfo.Models;
using AutomotiveInfo.News;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;

namespace AutomotiveInfo.Controllers;

/// <summary>
/// Backoffice-only editorial statistics, consumed by the editorial-stats backoffice dashboard.
/// Deriving from <see cref="ManagementApiControllerBase"/> puts the endpoint on the
/// Management API surface (/umbraco/management/api/v1/editorial-stats) behind
/// backoffice authentication — editorial data must not be publicly reachable.
/// </summary>
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("editorial-stats")]
[ApiExplorerSettings(GroupName = "Editorial Stats")]
[MapToApi("editorial-stats")]
public class EditorialStatsController : ManagementApiControllerBase
{
    private readonly INewsArticleService _newsArticleService;

    public EditorialStatsController(INewsArticleService newsArticleService)
    {
        _newsArticleService = newsArticleService;
    }

    /// <summary>
    /// Statistics aggregated server-side over the complete article list —
    /// clients must never compute stats from a capped/paged endpoint.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(NewsStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult GetStats([FromQuery] string? culture = null)
    {
        var resolvedCulture = _newsArticleService.ResolveCulture(culture);
        if (resolvedCulture is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Unknown culture",
                Detail = $"Culture '{culture}' is not configured for this site.",
            });
        }

        var allArticles = _newsArticleService.GetArticles(resolvedCulture)
                          ?? (IReadOnlyList<NewsArticleDto>)Array.Empty<NewsArticleDto>();

        var stats = new NewsStatsDto
        {
            TotalArticles = allArticles.Count,
            TagCounts = allArticles
                .GroupBy(a => a.Tag ?? string.Empty)
                .Select(g => new NewsTagCountDto { Tag = g.Key, Count = g.Count() })
                .OrderByDescending(t => t.Count)
                .ThenBy(t => t.Tag)
                .ToList(),
            // The cached list is already sorted by publish date descending.
            RecentArticles = allArticles.Take(5).ToList(),
        };

        return Ok(stats);
    }
}
