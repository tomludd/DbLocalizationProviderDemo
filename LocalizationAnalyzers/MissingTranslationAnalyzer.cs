using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LocalizationAnalyzers;

/// <summary>
/// LOC001: warns when a string property on a [LocalizedResource] or [LocalizedModel] class
/// is missing a [TranslationForCulture] for one of the required cultures.
///
/// Required cultures come from .editorconfig:
///     localization_required_cultures = no, sv
/// If not configured, they are inferred per class: any culture used on one property is required on all of them.
///
/// [TranslationForNorwegian], [TranslationForSwedish], [TranslationForEnglish] and [TranslationForFinnish] count too.
/// Properties are skipped when they have [UseResource] (translation lives on the target),
/// [Ignore], or are not of type string (nested resource models are checked on their own type).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingTranslationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LOC001";
    private const string ConfigKey = "localization_required_cultures";

    private const string Ns = "DbLocalizationProvider.Abstractions.";
    private const string LocalizedResourceAttr = Ns + "LocalizedResourceAttribute";
    private const string LocalizedModelAttr = Ns + "LocalizedModelAttribute";
    private const string TranslationForCultureAttr = Ns + "TranslationForCultureAttribute";
    private const string UseResourceAttr = Ns + "UseResourceAttribute";
    private const string IgnoreAttr = Ns + "IgnoreAttribute";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Missing translation",
        messageFormat: "'{0}.{1}' is missing a translation for culture '{2}'",
        category: "Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every localized string property should have a [TranslationForCulture] for each required culture.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!type.GetAttributes().Any(a => Is(a, LocalizedResourceAttr) || Is(a, LocalizedModelAttr)))
            return;

        var properties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public
                        && p.Type.SpecialType == SpecialType.System_String
                        && !p.GetAttributes().Any(a => Is(a, UseResourceAttr) || Is(a, IgnoreAttr)))
            .Select(p => (Property: p, Cultures: CulturesOf(p)))
            .ToList();

        if (properties.Count == 0) return;

        var required = ConfiguredCultures(context, type)
                       ?? properties.SelectMany(p => p.Cultures).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (property, cultures) in properties)
        {
            foreach (var culture in required.Where(c => !cultures.Contains(c)).OrderBy(c => c))
            {
                var location = property.Locations.FirstOrDefault() ?? Location.None;
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name, property.Name, culture));
            }
        }
    }

    // Shorthand attributes that derive from [TranslationForCulture] and fix the culture in their base() call
    private static readonly Dictionary<string, string> ShorthandCultures = new()
    {
        [Ns + "TranslationForEnglishAttribute"] = "en",
        [Ns + "TranslationForFinnishAttribute"] = "fi",
        [Ns + "TranslationForNorwegianAttribute"] = "no",
        [Ns + "TranslationForSwedishAttribute"] = "sv",
    };

    private static HashSet<string> CulturesOf(IPropertySymbol property)
    {
        var cultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in property.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            if (name == null) continue;

            if (name == TranslationForCultureAttr && attribute.ConstructorArguments.Length == 2)
            {
                if (attribute.ConstructorArguments[1].Value is string culture && !string.IsNullOrWhiteSpace(culture))
                    cultures.Add(culture);
            }
            else if (ShorthandCultures.TryGetValue(name, out var shorthand))
            {
                cultures.Add(shorthand);
            }
        }

        return cultures;
    }

    private static ImmutableHashSet<string>? ConfiguredCultures(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
        var tree = type.Locations.FirstOrDefault(l => l.IsInSource)?.SourceTree;
        if (tree == null) return null;

        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
        if (!options.TryGetValue(ConfigKey, out var value) || string.IsNullOrWhiteSpace(value))
            return null;

        return value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool Is(AttributeData attribute, string fullName) =>
        attribute.AttributeClass?.ToDisplayString() == fullName;
}
