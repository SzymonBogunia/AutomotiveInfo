using Umbraco.Cms.Core.Models.DeliveryApi;

namespace AutomotiveInfo.Models;

/// <summary>
/// The Delivery API rich-text shape, extended with a computed reading time.
/// Inherits <see cref="RichTextModel"/> so consumers lose nothing — markup
/// and nested blocks serialize exactly as the core shape does.
/// </summary>
public class RichTextWithReadingTime : RichTextModel
{
    public int ReadingTimeMinutes { get; init; }

    public static RichTextWithReadingTime FromModel(RichTextModel model, int readingTimeMinutes) =>
        new()
        {
            Markup = model.Markup,
            Blocks = model.Blocks,
            ReadingTimeMinutes = readingTimeMinutes,
        };
}
