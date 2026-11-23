using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using DbLocalizationProvider;
using DbLocalizationProvider.AspNetCore;
using LocalizationSample;
using LocalizationSample.Models;
using LocalizationSample.Resources;
using LocalizationSample.Storage;
using Microsoft.Extensions.DependencyInjection;

// =====================================================================================
// USAGE – setup is in Program.CreateProvider() at the bottom of this file
// =====================================================================================

var provider = Program.CreateProvider();
var json = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

// Single strings – strongly typed via expression (no magic strings)
CultureInfo.CurrentUICulture = new CultureInfo("no");
Console.WriteLine($"[no] Close        : {provider.GetString(() => GlobalResources.Close)}");
Console.WriteLine($"[no] FreeFreight  : {provider.GetString(() => CheckoutResources.FreeFreight, "250 kr")}");
Console.WriteLine($"[sv] Close        : {provider.GetString(() => GlobalResources.Close, new CultureInfo("sv"))}");

// Whole lang model in one call
Console.WriteLine("\n[no] TranslateExtended<CartLangModel>():");
Console.WriteLine(JsonSerializer.Serialize(provider.TranslateExtended<CartLangModel>(), json));

Console.WriteLine("\n[sv] TranslateExtended<CartLangModel>() – EmptyCartMessage has no sv translation, falls back to invariant (= property name). The analyzer warns about this at build time:");
Console.WriteLine(JsonSerializer.Serialize(provider.TranslateExtended<CartLangModel>(new CultureInfo("sv")), json));

// Built-in alternative – note Modal is null
Console.WriteLine("\n[no] Built-in Translate<CartLangModel>() – nested Modal is null:");
Console.WriteLine(JsonSerializer.Serialize(provider.Translate<CartLangModel>(), json));

// =====================================================================================
// SETUP – also used by the tests
// =====================================================================================

public partial class Program
{
    public static ILocalizationProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        // 1. Register – same call as in an ASP.NET Core Program.cs/Startup
        services.AddDbLocalizationProvider(ctx =>
        {
            ctx.EnableInvariantCultureFallback = true;
            ctx.UseInMemoryStorage();             // real app: ctx.UseSqlServer(connectionString);
        });

        var serviceProvider = services.BuildServiceProvider();

        // 2. Scan assemblies for [LocalizedResource]/[LocalizedModel] and sync translations to storage.
        //    In ASP.NET Core, app.UseDbLocalizationProvider() does exactly this.
        serviceProvider.UseDbLocalizationProvider();

        // 3. Resolve the provider – in an app you'd inject ILocalizationProvider instead
        return serviceProvider.GetRequiredService<ILocalizationProvider>();
    }
}
