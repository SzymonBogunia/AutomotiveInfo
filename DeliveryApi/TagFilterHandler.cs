using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;
using TagModel = Umbraco.Cms.Web.Common.PublishedModels.Tag;

namespace AutomotiveInfo.DeliveryApi;

/// <summary>
/// Adds tag filtering to the Delivery API: <c>?filter=tag:Premiera</c> (or several at once,
/// <c>?filter=tag:Premiera,Wypadek</c>). Values are matched case-insensitively.
/// No registration is needed — Umbraco discovers <c>IFilterHandler</c>/<c>IContentIndexHandler</c>
/// implementations by type scanning (verified empirically). The one required operational step:
/// the indexed <c>tagName</c> field only exists after the <c>DeliveryApiContentIndex</c> is
/// rebuilt (Settings → Examine Management), per the official docs.
/// </summary>
public class TagFilterHandler : IFilterHandler, IContentIndexHandler
{
    private const string TagSpecifier = "tag:";
    private const string FieldName = "tagName";

    // Property alias of the (invariant) tag picker on the article document type.
    private const string TagPropertyAlias = "tag";

    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public TagFilterHandler(IUmbracoContextFactory umbracoContextFactory)
    {
        _umbracoContextFactory = umbracoContextFactory;
    }

    // --- Querying: handles ?filter=tag:... ---

    public bool CanHandle(string query)
        => query.StartsWith(TagSpecifier, StringComparison.OrdinalIgnoreCase);

    public FilterOption BuildFilterOption(string filter)
    {
        var fieldValue = filter[TagSpecifier.Length..];

        // Values are lowercased to match the lowercased index field, making the
        // filter case-insensitive (?filter=tag:premiera == ?filter=tag:Premiera).
        // An empty value list (?filter=tag:) deliberately matches nothing.
        var values = fieldValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => v.ToLowerInvariant())
            .ToArray();

        return new FilterOption
        {
            FieldName = FieldName,
            Values = values,
            Operator = FilterOperation.Is
        };
    }

    // --- Indexing: projects the article's tag names into the Delivery API index ---

    public IEnumerable<IndexField> GetFields()
        => new[]
        {
            new IndexField
            {
                FieldName = FieldName,
                FieldType = FieldType.StringRaw,
                // The tag picker is invariant, so one value set serves every culture.
                VariesByCulture = false
            }
        };

    public IEnumerable<IndexFieldValue> GetFieldValues(IContent content, string? culture)
    {
        if (content.ContentType.Alias != NewsPage.ModelTypeAlias)
        {
            return Enumerable.Empty<IndexFieldValue>();
        }

        var tagNames = GetTagNames(content);

        if (tagNames.Count == 0)
        {
            return Enumerable.Empty<IndexFieldValue>();
        }

        return new[]
        {
            new IndexFieldValue
            {
                FieldName = FieldName,
                Values = tagNames.Cast<object>().ToArray()
            }
        };
    }

    private List<string> GetTagNames(IContent content)
    {
        var rawValue = content.GetValue<string>(TagPropertyAlias);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new List<string>();
        }

        var names = new List<string>();

        // Index population can run outside an HTTP request, so an Umbraco context is
        // ensured explicitly. Tag names are resolved from the published cache — the
        // previous IContentService lookups were one SQL round-trip per tag, per article,
        // per culture on every index rebuild.
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();

        foreach (var rawItem in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!UdiParser.TryParse(rawItem, out var udi) || udi is not GuidUdi guidUdi)
            {
                continue;
            }

            var tagName = (contextReference.UmbracoContext.Content?.GetById(guidUdi.Guid) as TagModel)?.TagName;

            if (!string.IsNullOrWhiteSpace(tagName))
            {
                // Stored lowercased; BuildFilterOption lowercases the queried values to match.
                names.Add(tagName.ToLowerInvariant());
            }
        }

        return names;
    }
}
