# DbLocalizationProviderDemo

A small, runnable example of [DbLocalizationProvider](https://github.com/valdisiljuconoks/LocalizationProvider)
(LocalizationProvider 9.1.0, .NET 10). It uses only `ILocalizationProvider`: no Admin UI, no CMS, no database.

```bash
dotnet run --project LocalizationSample
```

The build shows one intentional warning from the analyzer (`LOC001`, see below).

## Files

```
LocalizationSample/
  Program.cs                           setup and usage
  LocalizationProviderExtensions.cs    TranslateExtended<T>()
  InMemoryResourceRepository.cs        in-memory storage, replaces SQL Server in this demo
  Resources/                           resource classes and resource models
LocalizationAnalyzers/                 analyzer that warns about missing translations
```

## Setup

```csharp
services.AddDbLocalizationProvider(ctx =>
{
    ctx.EnableInvariantCultureFallback = true;
    ctx.UseSqlServer(connectionString);   // the demo uses ctx.UseInMemoryStorage()
});

app.UseDbLocalizationProvider();          // syncs translations from code to the database on startup
```

Only set `ctx.FlexibleRefactoringMode = true` if you use `[RenamedResource]`.

## Resource classes

```csharp
[LocalizedResource]
public class GlobalResources
{
    [TranslationForCulture("Lukk", "no")]
    [TranslationForCulture("Stäng", "sv")]
    public static string Close { get; set; }
}

provider.GetString(() => GlobalResources.Close);
```

## Resource models

A resource model holds the strings for one frontend component. Point properties at existing resources with
`[UseResource]`, and fill the whole model with `TranslateExtended<T>()`:

```csharp
[LocalizedModel]
public class CartResourceModel
{
    [UseResource(typeof(CheckoutResources), nameof(CheckoutResources.Item))]
    public string Item { get; set; }

    public ModalResourceModel Modal { get; set; }   // nested resource model
}

var model = provider.TranslateExtended<CartResourceModel>();
```

**Why not the built-in `Translate<T>()`?** It leaves nested resource models (`Modal`) as `null`. This is still the
case in 9.1.0. If your models never nest, `Translate<T>()` is enough.

## Analyzer: missing translations

`LOC001` warns at build time when a string in a `[LocalizedResource]` or `[LocalizedModel]` class is missing a
translation. Add it to your project:

```xml
<ProjectReference Include="..\LocalizationAnalyzers\LocalizationAnalyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Set the required cultures in `.editorconfig`:

```ini
[*.cs]
localization_required_cultures = no, sv
dotnet_diagnostic.LOC001.severity = error   # optional: fail the build
```
