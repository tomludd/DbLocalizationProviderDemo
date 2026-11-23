using DbLocalizationProvider.Abstractions;
using LocalizationSample.Resources;

namespace LocalizationSample.Models;

/// <summary>
/// A small "lang model" for a reusable component. Can be nested inside other lang models.
/// </summary>
[LocalizedModel]
public class ModalLangModel
{
    [UseResource(typeof(GlobalResources), nameof(GlobalResources.Close))]
    public string Close { get; set; }

    [UseResource(typeof(GlobalResources), nameof(GlobalResources.Ok))]
    public string Ok { get; set; }
}
