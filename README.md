# DbLocalizationProviderDemo

A small, runnable example of how to use [DbLocalizationProvider](https://github.com/valdisiljuconoks/LocalizationProvider)
(**LocalizationProvider 9.1.0**, .NET 10) with:

- **`ILocalizationProvider` only.** No Admin UI, no CMS integration, no SQL Server. Everything runs in memory.
- **Resource classes** (`[LocalizedResource]`): translations live in code as attributes and are synced to storage on startup.
- **Lang models** (`[LocalizedModel]`): flat classes of strings for frontend components, filled in one call with
  `TranslateExtended<T>()`.
- **A Roslyn analyzer** (`LOC001`) that warns at build time when a translation for a required culture is missing.

```bash
dotnet run --project LocalizationSample   # prints translated strings and lang models
```

Expected build output includes one intentional warning:

```
LocalizationSample/CheckoutResources.cs(23,26): warning LOC001: 'CheckoutResources.EmptyCartMessage' is missing a translation for culture 'sv'
```

## Layout

```
LocalizationSample/                    console app (net10.0), everything in one folder
  Program.cs                           setup (AddDbLocalizationProvider + sync) and usage
  LocalizationProviderExtensions.cs    TranslateExtended<T>()
  GlobalResources.cs                   [LocalizedResource] classes
  CheckoutResources.cs
  CartLangModel.cs                     [LocalizedModel] lang models
  ModalLangModel.cs
  InMemoryResourceRepository.cs        in-memory storage (only needed because we skip SQL Server)
LocalizationAnalyzers/                 Roslyn analyzer LOC001 (netstandard2.0)
.editorconfig                          analyzer configuration
```

Requires the .NET 10 SDK or newer.

## 1. Setup

```csharp
services.AddDbLocalizationProvider(ctx =>
{
    ctx.EnableInvariantCultureFallback = true;
    ctx.UseSqlServer(connectionString);   // this demo uses ctx.UseInMemoryStorage() instead
});

// ASP.NET Core:  app.UseDbLocalizationProvider();
// Console app:  serviceProvider.UseDbLocalizationProvider();
```

`UseDbLocalizationProvider()` scans assemblies for `[LocalizedResource]` and `[LocalizedModel]` and writes the
translations from code to storage. Editors can later override a translation in the database (Admin UI). A modified
resource is not overwritten by the code value on the next startup.

Packages: `LocalizationProvider` (core, `ILocalizationProvider`, attributes), `LocalizationProvider.AspNetCore`
(`AddDbLocalizationProvider()`), and in a real app `LocalizationProvider.Storage.SqlServer`.

### Do I need `FlexibleRefactoringMode`?

Only if you rename resource classes and mark them with `[RenamedResource(OldName = ..., OldNamespace = ...)]`. On startup,
the SQL storage moves the old key to the new one. With `FlexibleRefactoringMode = true`, it skips the move when the new
key already exists instead of failing. That happens when you switch between a branch with the rename and one without,
against the same database. The default is `false`. If you don't use `[RenamedResource]`, leave it out.

## 2. Resource classes

```csharp
[LocalizedResource]
public class GlobalResources
{
    [TranslationForCulture("Lukk", "no")]
    [TranslationForCulture("Stäng", "sv")]
    public static string Close { get; set; }
}
```

- The key is `<Namespace>.<Class>.<Property>`, here `LocalizationSample.GlobalResources.Close`.
- Use `static` properties so you can reference them in expressions: `provider.GetString(() => GlobalResources.Close)`.
- Placeholders: `provider.GetString(() => CheckoutResources.FreeFreight, "250 kr")`.
- Group resources by feature (`CheckoutResources`, `ProductResources`, ...) and keep one `GlobalResources` for shared words.
- `[TranslationForNorwegian("...")]`, `[TranslationForSwedish("...")]` and similar are shorthands for `[TranslationForCulture]`.

## 3. Lang models and `TranslateExtended<T>()`

A lang model holds the strings that one frontend component needs. Mark the class `[LocalizedModel]` and point its
properties at existing resources:

```csharp
[LocalizedModel]
public class CartLangModel
{
    [UseResource(typeof(CheckoutResources), nameof(CheckoutResources.Item))]
    public string Item { get; set; }

    [TranslationForCulture("Andre så også på", "no")]   // string used only by this model
    [TranslationForCulture("Andra tittade också på", "sv")]
    public string Recommendations { get; set; }

    public ModalLangModel Modal { get; set; }            // nested [LocalizedModel], filled recursively
}

var lang   = localizationProvider.TranslateExtended<CartLangModel>();                    // CurrentUICulture
var langSv = localizationProvider.TranslateExtended<CartLangModel>(new CultureInfo("sv"));
```

Prefer `[UseResource]` over repeating `[TranslationForCulture]`. One translation can then be reused by many components.

### Why `TranslateExtended` exists

DbLocalizationProvider has a built-in `ILocalizationProvider.Translate<T>()` that does nearly the same job. It reads
every resource whose key starts with `typeof(T).FullName`, builds JSON from them and deserializes it. A nested lang
model has keys under its own type name, so it is not included:

| | `TranslateExtended<T>()` | built-in `Translate<T>()` |
|---|---|---|
| Inline `[TranslationForCulture]` on the model | ✅ | ✅ |
| `[UseResource]` properties | ✅ | ✅ |
| Explicit culture | ✅ | ✅ |
| Nested `[LocalizedModel]` property (e.g. `Modal`) | filled | **`null`** |
| `[Display(Name)]` fallback | ✅ | ❌ |

This is still the case in 9.1.0. `Program.cs` prints both, so after a package upgrade you can see whether
`Modal` is still `null`. If your lang models never nest, the built-in
`Translate<T>()` is enough.

How `TranslateExtended` resolves each property:

| Property                         | Resource key used                                   |
|----------------------------------|-----------------------------------------------------|
| `string` with `[UseResource]`    | `<TargetContainer.FullName>.<PropertyName>`         |
| `string` without                 | `<Model.FullName>.<Property>`                       |
| type marked `[LocalizedModel]`   | recursive `TranslateExtended`                       |
| no translation found             | `[Display(Name = "...")]` if present                |

## 4. Analyzer: `LOC001 Missing translation`

`MissingTranslationAnalyzer` checks every class marked `[LocalizedResource]` or `[LocalizedModel]`. It reports each
public `string` property that has no translation attribute for one of the required cultures. Both
`[TranslationForCulture]` and the shorthand attributes count. Properties with `[UseResource]` or `[Ignore]` are skipped,
because the resource they point to is checked instead.

Reference it from the project that holds the resources:

```xml
<ProjectReference Include="..\LocalizationAnalyzers\LocalizationAnalyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Set the required cultures in `.editorconfig`:

```ini
[*.cs]
localization_required_cultures = no, sv

# fail the build instead of warning
dotnet_diagnostic.LOC001.severity = error
```

If `localization_required_cultures` is not set, the analyzer works out the cultures per class: a culture used on one
property is required on every property in that class. That catches the common mistake of adding Norwegian and
forgetting Swedish.

The analyzer references Microsoft.CodeAnalysis 5.0.0 on purpose. A compiler can't load an analyzer built against a
newer Roslyn than itself, and 5.0.0 is the version in the first .NET 10 SDK. To share it across solutions, pack
`LocalizationAnalyzers` as a NuGet package with the DLL under `analyzers/dotnet/cs/`.

## Upgrading from 8.x

- 9.x targets **net10.0 only**.
- `IResourceRepository` changed. If you have your own storage, it now needs `DeleteResources(...)`, and
  `RegisterDiscoveredResources` takes a `Dictionary<string, LocalizationResource>` and a `SyncSource`.
  With `UseSqlServer` you don't need to do anything.
- Resource attributes, `ILocalizationProvider` and `Translate<T>()` work the same way.
