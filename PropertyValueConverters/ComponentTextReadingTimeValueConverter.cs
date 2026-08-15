using AutomotiveInfo.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.DeliveryApi;
using Umbraco.Cms.Core.Strings;

namespace AutomotiveInfo.PropertyValueConverters;

public class ComponentTextReadingTimeValueConverter : PropertyValueConverterBase, IDeliveryApiPropertyValueConverter
{
    private const int WordsPerMinute = 200;

    public override bool IsConverter(IPublishedPropertyType propertyType)
        => propertyType.Alias.Equals("componentText", StringComparison.OrdinalIgnoreCase);

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
        => typeof(IHtmlEncodedString);

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
        => PropertyCacheLevel.Element;

    private static string ExtractMarkup(object? sourceValue)
    {
        if (sourceValue is not string json || string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("markup", out var markupProp))
            {
                return markupProp.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
            return json;
        }

        return string.Empty;
    }

    private static int CalculateReadingTime(string markup)
    {
        var plainText = Regex.Replace(markup, "<.*?>", " ");
        var wordCount = plainText.Split(
            new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    public override object? ConvertIntermediateToObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview)
    {
        var markup = ExtractMarkup(inter);
        return new HtmlEncodedString(markup);
    }

    public PropertyCacheLevel GetDeliveryApiPropertyCacheLevel(IPublishedPropertyType propertyType)
        => PropertyCacheLevel.Element;

    public Type GetDeliveryApiPropertyValueType(IPublishedPropertyType propertyType)
        => typeof(RichTextWithReadingTime);

    public object? ConvertIntermediateToDeliveryApiObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview,
        bool expanding)
    {
        var markup = ExtractMarkup(inter);
        return new RichTextWithReadingTime
        {
            Markup = markup,
            ReadingTimeMinutes = CalculateReadingTime(markup)
        };
    }
}