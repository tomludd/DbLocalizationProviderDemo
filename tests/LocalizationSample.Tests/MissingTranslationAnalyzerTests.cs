using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DbLocalizationProvider.Abstractions;
using LocalizationAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace LocalizationSample.Tests;

public class MissingTranslationAnalyzerTests
{
    [Fact]
    public async Task Reports_cultures_required_by_editorconfig()
    {
        const string source = """
            using DbLocalizationProvider.Abstractions;

            [LocalizedResource]
            public class Resources
            {
                [TranslationForCulture("Lukk", "no")]
                public static string Close { get; set; }
            }
            """;

        var diagnostics = await Analyze(source, requiredCultures: "no, sv, en");

        Assert.Equal(
            ["'Resources.Close' is missing a translation for culture 'en'",
             "'Resources.Close' is missing a translation for culture 'sv'"],
            diagnostics.Select(d => d.GetMessage()));
        Assert.All(diagnostics, d => Assert.Equal("LOC001", d.Id));
    }

    [Fact]
    public async Task Infers_required_cultures_per_class_when_not_configured()
    {
        const string source = """
            using DbLocalizationProvider.Abstractions;

            [LocalizedModel]
            public class LangModel
            {
                [TranslationForCulture("Lukk", "no")]
                [TranslationForCulture("Stäng", "sv")]
                public string Close { get; set; }

                [TranslationForCulture("Åpne", "no")]
                public string Open { get; set; }
            }
            """;

        var diagnostics = await Analyze(source, requiredCultures: null);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("'LangModel.Open' is missing a translation for culture 'sv'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Counts_shorthand_attributes()
    {
        const string source = """
            using DbLocalizationProvider.Abstractions;

            [LocalizedResource]
            public class Resources
            {
                [TranslationForNorwegian("Lukk")]
                [TranslationForSwedish("Stäng")]
                public static string Close { get; set; }
            }
            """;

        Assert.Empty(await Analyze(source, requiredCultures: "no, sv"));
    }

    [Fact]
    public async Task Skips_UseResource_Ignore_and_non_string_properties()
    {
        const string source = """
            using DbLocalizationProvider.Abstractions;

            [LocalizedResource]
            public class Shared
            {
                [TranslationForCulture("Lukk", "no")]
                [TranslationForCulture("Stäng", "sv")]
                public static string Close { get; set; }
            }

            [LocalizedModel]
            public class Nested { }

            [LocalizedModel]
            public class LangModel
            {
                [UseResource(typeof(Shared), nameof(Shared.Close))]
                public string Close { get; set; }

                [Ignore]
                public string NotTranslated { get; set; }

                public Nested Child { get; set; }
            }
            """;

        Assert.Empty(await Analyze(source, requiredCultures: "no, sv"));
    }

    [Fact]
    public async Task Ignores_classes_that_are_not_localized()
    {
        const string source = """
            public class PlainDto
            {
                public string Close { get; set; }
            }
            """;

        Assert.Empty(await Analyze(source, requiredCultures: "no, sv"));
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string source, string requiredCultures)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(LocalizedResourceAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            [CSharpSyntaxTree.ParseText(source, path: "Test.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compileErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(compileErrors.Count == 0, string.Join(Environment.NewLine, compileErrors));

        var options = new Dictionary<string, string>();
        if (requiredCultures != null) options["localization_required_cultures"] = requiredCultures;

        var diagnostics = await compilation
            .WithAnalyzers(
                [new MissingTranslationAnalyzer()],
                new AnalyzerOptions([], new TestOptionsProvider(options)))
            .GetAnalyzerDiagnosticsAsync();

        return [.. diagnostics.OrderBy(d => d.GetMessage())];
    }

    /// <summary>Feeds .editorconfig-style key/values to the analyzer.</summary>
    private sealed class TestOptionsProvider(Dictionary<string, string> values) : AnalyzerConfigOptionsProvider
    {
        private readonly TestOptions _options = new(values);
        public override AnalyzerConfigOptions GlobalOptions => _options;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;

        private sealed class TestOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [NotNullWhen(true)] out string value) =>
                values.TryGetValue(key, out value);
        }
    }
}
