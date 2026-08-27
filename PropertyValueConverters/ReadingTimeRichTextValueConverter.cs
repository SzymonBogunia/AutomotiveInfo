using System.Net;
using System.Text.RegularExpressions;
using AutomotiveInfo.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.DeliveryApi;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;

namespace AutomotiveInfo.PropertyValueConverters;

/// <summary>
/// Extends the Delivery API output of every rich-text property with a computed reading time,
/// by decorating the core <see cref="RteBlockRenderingValueConverter"/> (composition, not
/// inheritance — the core ctor churns between minor versions; DI resolves it for us).
///
/// The Razor path is pure delegation, so local links, media URLs and nested RTE blocks keep
/// the core behaviour — the previous implementation returned raw stored markup and broke all
/// three. Matching is delegated too (editor-based, all RTE properties), so the output shape
/// is consistent across <c>componentText</c>, <c>imageWithTextBlock.text</c> and any future
/// rich-text property — instead of being keyed to one property alias.
///
/// No registration needed: property value converters are type-scanned, and a converter
/// without [DefaultPropertyValueConverter] takes precedence over the core (attributed) one.
/// </summary>
public partial class ReadingTimeRichTextValueConverter : PropertyValueConverterBase, IDeliveryApiPropertyValueConverter
{
    private const int WordsPerMinute = 200;

    private readonly RteBlockRenderingValueConverter _coreConverter;

    public ReadingTimeRichTextValueConverter(RteBlockRenderingValueConverter coreConverter)
    {
        _coreConverter = coreConverter;
    }

    // --- Razor / typed-model path: pure delegation to the core converter ---

    public override bool IsConverter(IPublishedPropertyType propertyType)
        => _coreConverter.IsConverter(propertyType);

    public override bool? IsValue(object? value, PropertyValueLevel level)
        => _coreConverter.IsValue(value, level);

    public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
        => _coreConverter.GetPropertyValueType(propertyType);

    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
        => _coreConverter.GetPropertyCacheLevel(propertyType);

    public override object? ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType propertyType, object? source, bool preview)
        => _coreConverter.ConvertSourceToIntermediate(owner, propertyType, source, preview);

    public override object? ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
        => _coreConverter.ConvertIntermediateToObject(owner, propertyType, referenceCacheLevel, inter, preview);

    // --- Delivery API path: delegate to the core converter, then decorate ---

    public PropertyCacheLevel GetDeliveryApiPropertyCacheLevel(IPublishedPropertyType propertyType)
        => _coreConverter.GetDeliveryApiPropertyCacheLevel(propertyType);

    public PropertyCacheLevel GetDeliveryApiPropertyCacheLevelForExpansion(IPublishedPropertyType propertyType)
        => _coreConverter.GetDeliveryApiPropertyCacheLevelForExpansion(propertyType);

    public Type GetDeliveryApiPropertyValueType(IPublishedPropertyType propertyType)
        => typeof(RichTextWithReadingTime);

    public object? ConvertIntermediateToDeliveryApiObject(IPublishedElement owner, IPublishedPropertyType propertyType, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview, bool expanding)
    {
        var coreModel = _coreConverter.ConvertIntermediateToDeliveryApiObject(
            owner, propertyType, referenceCacheLevel, inter, preview, expanding) as RichTextModel;

        if (coreModel is null)
        {
            return null;
        }

        // The reading time is computed from the *processed* markup (local links and media
        // already resolved by the core converter), not from the raw stored JSON.
        return RichTextWithReadingTime.FromModel(coreModel, CalculateReadingTimeMinutes(coreModel.Markup));
    }

    private static int CalculateReadingTimeMinutes(string markup)
    {
        if (string.IsNullOrWhiteSpace(markup))
        {
            return 0;
        }

        // Strip tags, then decode entities so "&nbsp;&mdash;" doesn't count as words.
        var plainText = WebUtility.HtmlDecode(HtmlTagRegex().Replace(markup, " "));

        var wordCount = plainText.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Length;

        return wordCount == 0
            ? 0
            : Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    // Source-generated: compiled once, Singleline so tags spanning newlines are stripped,
    // and a timeout bounds worst-case editor-supplied content.
    [GeneratedRegex("<.*?>", RegexOptions.Singleline, matchTimeoutMilliseconds: 250)]
    private static partial Regex HtmlTagRegex();
}
