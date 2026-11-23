using System.Globalization;
using DbLocalizationProvider;
using DbLocalizationProvider.Abstractions;
using LocalizationSample.Models;
using Xunit;

namespace LocalizationSample.Tests;

public class TranslateExtendedTests
{
    private static readonly Lazy<ILocalizationProvider> LazyProvider = new(LocalizationSetup.CreateProvider);
    private static ILocalizationProvider Provider => LazyProvider.Value;

    private static readonly CultureInfo Norwegian = new("no");
    private static readonly CultureInfo Swedish = new("sv");

    [Fact]
    public void Fills_UseResource_and_inline_translations()
    {
        var model = Provider.TranslateExtended<CartLangModel>(Norwegian);

        Assert.Equal("Vare", model.Item);                          // [UseResource] -> CheckoutResources.Item
        Assert.Equal("Avbryt", model.Cancel);                      // [UseResource] -> GlobalResources.Cancel
        Assert.Equal("Andre så også på", model.Recommendations);   // inline [TranslationForCulture]
    }

    [Fact]
    public void Fills_nested_lang_models()
    {
        var model = Provider.TranslateExtended<CartLangModel>(Swedish);

        Assert.NotNull(model.Modal);
        Assert.Equal("Stäng", model.Modal.Close);
        Assert.Equal("OK", model.Modal.Ok);
    }

    [Fact]
    public void Uses_CurrentUICulture_when_no_culture_is_given()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = Swedish;
            Assert.Equal("Vara", Provider.TranslateExtended<CartLangModel>().Item);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Missing_translation_falls_back_to_invariant()
    {
        // CheckoutResources.EmptyCartMessage has no "sv" translation (the analyzer warns about it).
        // With EnableInvariantCultureFallback the invariant value is used, which defaults to the property name.
        var model = Provider.TranslateExtended<CartLangModel>(Swedish);

        Assert.Equal("EmptyCartMessage", model.EmptyCartMessage);
    }

    [Fact]
    public void Throws_for_types_without_LocalizedModel()
    {
        Assert.Throws<NotSupportedException>(() => Provider.TranslateExtended<NotALangModel>());
    }

    /// <summary>
    /// Documents why TranslateExtended exists. If this test starts failing after a package upgrade,
    /// the built-in Translate&lt;T&gt;() fills nested models and TranslateExtended may no longer be needed.
    /// </summary>
    [Fact]
    public void BuiltIn_Translate_does_not_fill_nested_lang_models()
    {
        var model = Provider.Translate<CartLangModel>(Norwegian);

        Assert.Equal("Vare", model.Item);   // flat strings work, also [UseResource]
        Assert.Null(model.Modal);           // ...but nested [LocalizedModel] is not filled
    }

    public class NotALangModel
    {
        public string Text { get; set; }
    }
}
