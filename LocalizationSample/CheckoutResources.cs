using DbLocalizationProvider.Abstractions;

namespace LocalizationSample;

[LocalizedResource]
public class CheckoutResources
{
    [TranslationForCulture("Vare", "no")]
    [TranslationForCulture("Vara", "sv")]
    public static string Item { get; set; }

    [TranslationForCulture("Varer", "no")]
    [TranslationForCulture("Varor", "sv")]
    public static string Items { get; set; }

    // Placeholders are fine – format at the call site with string.Format / GetString(..., args)
    [TranslationForCulture("{0} igjen til fri frakt", "no")]
    [TranslationForCulture("{0} kvar till fri frakt", "sv")]
    public static string FreeFreight { get; set; }

    // Deliberately missing the Swedish translation -> analyzer LOC001 warns at build time.
    [TranslationForCulture("Handlekurven er tom", "no")]
    public static string EmptyCartMessage { get; set; }
}
