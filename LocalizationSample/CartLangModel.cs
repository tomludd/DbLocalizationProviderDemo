using DbLocalizationProvider.Abstractions;
using LocalizationSample.Resources;

namespace LocalizationSample;

/// <summary>
/// "Lang model": a flat bag of strings handed to a frontend component (e.g. serialized to JSON).
/// Filled in one go with <c>localizationProvider.TranslateExtended&lt;CartLangModel&gt;()</c>.
///
/// Each property is either
///  - pointed at an existing resource with [UseResource] (preferred, avoids duplicate translations), or
///  - translated inline with [TranslationForCulture] (key: LocalizationSample.Models.CartLangModel.PropName), or
///  - a nested [LocalizedModel] which is filled recursively.
/// </summary>
[LocalizedModel]
public class CartLangModel
{
    [UseResource(typeof(CheckoutResources), nameof(CheckoutResources.Item))]
    public string Item { get; set; }

    [UseResource(typeof(CheckoutResources), nameof(CheckoutResources.Items))]
    public string Items { get; set; }

    [UseResource(typeof(CheckoutResources), nameof(CheckoutResources.FreeFreight))]
    public string FreeFreight { get; set; }

    [UseResource(typeof(CheckoutResources), nameof(CheckoutResources.EmptyCartMessage))]
    public string EmptyCartMessage { get; set; }

    [UseResource(typeof(GlobalResources), nameof(GlobalResources.Cancel))]
    public string Cancel { get; set; }

    // Model-specific string, translated inline
    [TranslationForCulture("Andre så også på", "no")]
    [TranslationForCulture("Andra tittade också på", "sv")]
    public string Recommendations { get; set; }

    // Nested lang model – filled recursively by TranslateExtended
    public ModalLangModel Modal { get; set; }
}
