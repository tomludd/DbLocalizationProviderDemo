using DbLocalizationProvider.Abstractions;

namespace LocalizationSample.Resources;

/// <summary>
/// A small "resource model" for a reusable component. Can be nested inside other resource models.
/// </summary>
[LocalizedModel]
public class ModalResourceModel
{
    [UseResource(typeof(GlobalResources), nameof(GlobalResources.Close))]
    public string Close { get; set; }

    [UseResource(typeof(GlobalResources), nameof(GlobalResources.Ok))]
    public string Ok { get; set; }
}
