using AutomotiveInfo.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace AutomotiveInfo.Search;

/// <summary>
/// View model for the search page: wraps the page node (so the layout and traversal
/// keep working against <see cref="IPublishedContent"/>) and carries the search state,
/// keeping query parsing and service calls out of the Razor view.
/// </summary>
public class SearchViewModel : PublishedContentWrapped
{
    public SearchViewModel(IPublishedContent content, IPublishedValueFallback publishedValueFallback)
        : base(content, publishedValueFallback)
    {
    }

    /// <summary>The raw (trimmed) search phrase the visitor typed, or empty.</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>The active tag filter, or empty when browsing without one.</summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>Tags available to filter by, with article counts.</summary>
    public IReadOnlyList<TagFacet> TagFacets { get; init; } = Array.Empty<TagFacet>();

    /// <summary>Search results — null when neither a phrase nor a tag was submitted.</summary>
    public ArticleSearchResult? Results { get; init; }
}
