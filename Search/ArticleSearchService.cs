using Examine;
using Examine.Search;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using static Umbraco.Cms.Core.Constants;

namespace AutomotiveInfo.Search;

public record ArticleSearchResult(
    IReadOnlyList<IPublishedContent> Items,
    long TotalResults,
    int TotalPages,
    int PageNumber);

public interface IArticleSearchService
{
    ArticleSearchResult Search(string query, int pageNumber, int pageSize);
}

public class ArticleSearchService : IArticleSearchService
{
    private const string NewsPageAlias = "newsPage";
    private const string TagAlias = "tag";

    // Pola przeszukiwane w ExternalIndex.
    // Sufiks "_pl-pl" wynika z tego, że newsPage jest wariantowy kulturowo
    // (__VariesByCulture: y) - Examine dokleja kod kultury do nazwy pola.
    // Jeśli w przyszłości dojdzie więcej języków, tę listę trzeba będzie
    // budować dynamicznie na podstawie aktualnej kultury żądania.
    private static readonly string[] SearchFields =
    {
        "nodeName",
        "articleTitle_pl-pl",
        "components_pl-pl",
    };

    private readonly IExamineManager _examineManager;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public ArticleSearchService(IExamineManager examineManager, IUmbracoContextFactory umbracoContextFactory)
    {
        _examineManager = examineManager;
        _umbracoContextFactory = umbracoContextFactory;
    }

    public ArticleSearchResult Search(string query, int pageNumber, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query) ||
            !_examineManager.TryGetIndex(UmbracoIndexes.ExternalIndexName, out IIndex? index))
        {
            return new ArticleSearchResult(Array.Empty<IPublishedContent>(), 0, 0, pageNumber);
        }

        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        var skip = (pageNumber - 1) * pageSize;

        var trimmed = query.Trim();

        IBooleanOperation? booleanQuery = trimmed.StartsWith('#')
            ? BuildTagQuery(index, trimmed.TrimStart('#').Trim())
            : BuildTextQuery(index, trimmed);

        if (booleanQuery is null)
        {
            return new ArticleSearchResult(Array.Empty<IPublishedContent>(), 0, 0, pageNumber);
        }

        ISearchResults results = booleanQuery.Execute(QueryOptions.SkipTake(skip, pageSize));

        var totalResults = results.TotalItemCount;
        var totalPages = (int)Math.Ceiling(totalResults / (double)pageSize);

        using UmbracoContextReference cref = _umbracoContextFactory.EnsureUmbracoContext();

        var items = results
            .Select(r => int.TryParse(r.Id, out var id) ? cref.UmbracoContext.Content?.GetById(id) : null)
            .Where(c => c != null)
            .Cast<IPublishedContent>()
            .ToList();

        Console.WriteLine($"[DEBUG] nativeQuery result count: {results.TotalItemCount}");

        return new ArticleSearchResult(items, totalResults, totalPages, pageNumber);
    }

    private static IBooleanOperation BuildTextQuery(IIndex index, string term)
    {
        return index.Searcher
            .CreateQuery("content")
            .NodeTypeAlias(NewsPageAlias)
            .And()
            .ManagedQuery(term, SearchFields);
    }

    private static IBooleanOperation? BuildTagQuery(IIndex index, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        var tagUdis = GetTagUdis(index, tagName);

        if (tagUdis.Length == 0)
        {
            return null;
        }

        // Budujemy zapytanie Lucene w postaci: +(tag:"umb://..." tag:"umb://...")
        // Wymusza to, że artykuł MUSI posiadać przynajmniej jeden z tych UDI w polu 'tag'
        var tagClauses = string.Join(" OR ", tagUdis.Select(u => $"tag:\"{u}\""));
        var rawQuery = $"+__NodeTypeAlias:{NewsPageAlias} +({tagClauses})";

        return index.Searcher
            .CreateQuery("content")
            .NativeQuery(rawQuery);
    }

    private static string[] GetTagUdis(IIndex index, string tagName)
    {
        ISearchResults tagNodeResults = index.Searcher
            .CreateQuery("content")
            .NodeTypeAlias(TagAlias)
            .And()
            .Field("nodeName", tagName)
            .Execute();

        return tagNodeResults
            .Select(r => r.Values.TryGetValue("__Key", out var key) ? key : null)
            .Where(k => !string.IsNullOrEmpty(k))
            .Select(k => $"umb://document/{k!.Replace("-", "").ToLowerInvariant()}")
            .Distinct()
            .ToArray();
    }
}