using AutomotiveInfo.News;
using AutomotiveInfo.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace AutomotiveInfo.Controllers;

/// <summary>
/// Route-hijacking controller for the <c>searchPage</c> document type: owns the HTTP
/// concerns (query-string parsing, paging defaults) and hands the view a ready model,
/// so the Razor template contains no service calls or request parsing.
/// </summary>
public class SearchPageController : RenderController
{
    private const int FallbackPageSize = 5;
    private const int MaxPageSize = 50;

    private readonly IArticleSearchService _articleSearchService;
    private readonly INewsArticleService _newsArticleService;
    private readonly IPublishedValueFallback _publishedValueFallback;

    public SearchPageController(
        ILogger<SearchPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        IArticleSearchService articleSearchService,
        INewsArticleService newsArticleService,
        IPublishedValueFallback publishedValueFallback)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _articleSearchService = articleSearchService;
        _newsArticleService = newsArticleService;
        _publishedValueFallback = publishedValueFallback;
    }

    public override IActionResult Index()
    {
        if (CurrentPage is null)
        {
            return NotFound();
        }

        var query = Request.Query["q"].ToString().Trim();
        var tag = Request.Query["tag"].ToString().Trim();
        var page = int.TryParse(Request.Query["page"], out var parsed) && parsed > 0 ? parsed : 1;

        // Editors control the page size on the search page itself; clamp defensively.
        var configured = (CurrentPage as SearchPage)?.ResultsPerPage ?? 0;
        var pageSize = configured > 0 ? Math.Clamp(configured, 1, MaxPageSize) : FallbackPageSize;

        var hasCriteria = !string.IsNullOrWhiteSpace(query) || !string.IsNullOrWhiteSpace(tag);
        var results = hasCriteria
            ? _articleSearchService.Search(query, tag, page, pageSize)
            : null;

        var culture = _newsArticleService.ResolveCulture(null)!;

        var viewModel = new SearchViewModel(CurrentPage, _publishedValueFallback)
        {
            Query = query,
            Tag = tag,
            TagFacets = _newsArticleService.GetTagFacets(culture),
            Results = results,
        };

        return CurrentTemplate(viewModel);
    }
}
