using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace AutomotiveInfo.DeliveryApi;

public class TagFilterHandler : IFilterHandler, IContentIndexHandler
{
    private const string TagSpecifier = "tag:";
    private const string FieldName = "tagName";

    private readonly IContentService _contentService;

    public TagFilterHandler(IContentService contentService)
    {
        _contentService = contentService;
    }

    // --- Querying: obsługa ?filter=tag:Premiera ---

    public bool CanHandle(string query)
        => query.StartsWith(TagSpecifier, StringComparison.OrdinalIgnoreCase);

    public FilterOption BuildFilterOption(string filter)
    {
        var fieldValue = filter[TagSpecifier.Length..];

        // Wiele tagów naraz: ?filter=tag:Premiera,Wypadek
        var values = fieldValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new FilterOption
        {
            FieldName = FieldName,
            Values = values,
            Operator = FilterOperation.Is
        };
    }

    // --- Indexing: jak wyciągnąć nazwę tagu i wrzucić ją do indeksu ---

    public IEnumerable<IndexField> GetFields()
        => new[]
        {
            new IndexField
            {
                FieldName = FieldName,
                FieldType = FieldType.StringRaw,
                VariesByCulture = false
            }
        };

    public IEnumerable<IndexFieldValue> GetFieldValues(IContent content, string? culture)
    {
        if (content.ContentType.Alias != "newsPage")
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

        var rawValue = content.GetValue<string>("tag");

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new List<string>();
        }

        var names = new List<string>();

        foreach (var rawItem in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!UdiParser.TryParse(rawItem, out var udi) || udi is not GuidUdi guidUdi)
            {
                continue;
            }

            var tagContent = _contentService.GetById(guidUdi.Guid);
            var tagName = tagContent?.GetValue<string>("tagName");

            if (!string.IsNullOrWhiteSpace(tagName))
            {
                names.Add(tagName);
            }
        }

        return names;
    }
}