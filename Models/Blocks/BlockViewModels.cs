using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace AutomotiveInfo.Models.Blocks;

// Presentation-ready view models for the shared block partials
// (Views/Partials/components/_*.cshtml). The Block List and Block Grid wrappers
// both map onto these, so the markup exists exactly once and cannot drift.

/// <summary>
/// Presentation options every block shares, projected from the <see cref="BlockSettings"/>
/// settings element: vertical rhythm, an optional in-page anchor and an optional background.
/// Computed once here so no view has to interpret raw setting values.
/// </summary>
public sealed record BlockChrome(
    string SpacingClasses,
    string? AnchorId,
    string? BackgroundStyle,
    string BackgroundPaddingClasses)
{
    public static readonly BlockChrome Default = new("mt-12 mb-12", null, null, string.Empty);
}

public sealed record RichTextViewModel(IHtmlEncodedString? Text, BlockChrome Chrome);

public sealed record CtaViewModel(string? Header, string? Lead, Link? Link, BlockChrome Chrome);

/// <summary>Position is a canonical, language-independent key: "left" | "center" | "right".</summary>
public sealed record ImageWithTextViewModel(MediaWithCrops? Image, IHtmlEncodedString? Text, string Position, BlockChrome Chrome);

public sealed record AccordionItemViewModel(string Heading, IHtmlEncodedString? Content);

public sealed record AccordionViewModel(string? Title, IReadOnlyList<AccordionItemViewModel> Items, BlockChrome Chrome);

public sealed record RecentNewsViewModel(string? Title, string? ViewAllUrl, IReadOnlyList<NewsArticleDto> Articles, BlockChrome Chrome);

/// <summary>
/// Maps generated block models to the shared view models — one place, used by
/// both the Block List and Block Grid wrapper views.
/// </summary>
public static class BlockViewModelFactory
{
    public static RichTextViewModel ToViewModel(this RichTextBlock block, BlockSettings? settings) =>
        new(block.ComponentText, ToChrome(settings));

    public static CtaViewModel ToViewModel(this CallToActionBlock block, BlockSettings? settings) => new(
        block.Header,
        block.Lead,
        block.Link?.FirstOrDefault(),
        // CTA renders as a card, so it supplies its own padding — but if the editor
        // picked no background we still give it the default surface colour.
        ToChrome(settings) with { BackgroundStyle = ToChrome(settings).BackgroundStyle ?? "background-color: #0f172a;" });

    public static ImageWithTextViewModel ToViewModel(this ImageWithTextBlock block, BlockSettings? settings) => new(
        block.Image as MediaWithCrops,
        block.Text,
        MapPositionKey(block.ImagePosition),
        ToChrome(settings));

    public static AccordionViewModel ToViewModel(this Accordion block, BlockSettings? settings) => new(
        block.Title,
        block.Items?
            .Select(i => i.Content)
            .OfType<AccordionBlock>()
            .Where(a => !string.IsNullOrEmpty(a.Heading))
            .Select(a => new AccordionItemViewModel(a.Heading!, a.Content))
            .ToList()
            ?? (IReadOnlyList<AccordionItemViewModel>)Array.Empty<AccordionItemViewModel>(),
        ToChrome(settings));

    /// <summary>Projects the shared settings element onto presentation-ready chrome.</summary>
    public static BlockChrome ToChrome(BlockSettings? settings)
    {
        if (settings is null)
        {
            return BlockChrome.Default;
        }

        var backgroundHex = NormalizeHex(settings.BackgroundColor?.Color);
        var anchorId = string.IsNullOrWhiteSpace(settings.AnchorId) ? null : settings.AnchorId;

        return new BlockChrome(
            SpacingClasses: $"{TopSpacingClass(settings.SpacingTop)} {BottomSpacingClass(settings.SpacingBottom)}",
            AnchorId: anchorId,
            BackgroundStyle: backgroundHex is null ? null : $"background-color: {backgroundHex};",
            // A background needs breathing room; only added when one is actually chosen.
            BackgroundPaddingClasses: backgroundHex is null ? string.Empty : "p-8 md:p-12 rounded-2xl");
    }

    // Literal class names (never composed at runtime) so a real Tailwind build
    // can see them during purging.
    private static string TopSpacingClass(string? spacing) => spacing switch
    {
        "none" => "mt-0",
        "small" => "mt-6",
        "large" => "mt-24",
        _ => "mt-12",
    };

    private static string BottomSpacingClass(string? spacing) => spacing switch
    {
        "none" => "mb-0",
        "small" => "mb-6",
        "large" => "mb-24",
        _ => "mb-12",
    };

    // The colour picker's *value* (hex) is the stable key — labels are editor-facing
    // Polish names and must never drive rendering.
    private static string? NormalizeHex(string? hex) =>
        string.IsNullOrWhiteSpace(hex)
            ? null
            : hex.StartsWith('#') ? hex : "#" + hex;

    // "Prawo"/"Centruj"/"Lewo" are legacy values stored before the invariant-key
    // migration of the imagePosition dropdown; they disappear as content is resaved.
    private static string MapPositionKey(string? stored) => stored switch
    {
        "right" or "Prawo" => "right",
        "center" or "Centruj" => "center",
        _ => "left",
    };
}
