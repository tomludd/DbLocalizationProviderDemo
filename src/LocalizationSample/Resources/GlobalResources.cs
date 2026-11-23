using DbLocalizationProvider.Abstractions;

namespace LocalizationSample.Resources;

/// <summary>
/// Shared strings reused across many models via [UseResource].
/// Resource keys become "LocalizationSample.Resources.GlobalResources.Ok" etc.
/// </summary>
[LocalizedResource]
public class GlobalResources
{
    [TranslationForCulture("OK", "no")]
    [TranslationForCulture("OK", "sv")]
    public static string Ok { get; set; }

    [TranslationForCulture("Lukk", "no")]
    [TranslationForCulture("Stäng", "sv")]
    public static string Close { get; set; }

    [TranslationForCulture("Avbryt", "no")]
    [TranslationForCulture("Avbryt", "sv")]
    public static string Cancel { get; set; }

    [TranslationForCulture("Noe gikk galt", "no")]
    [TranslationForCulture("Något gick fel", "sv")]
    public static string SomethingWentWrong { get; set; }
}
