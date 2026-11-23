using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using DbLocalizationProvider;
using DbLocalizationProvider.Abstractions;

namespace LocalizationSample;

public static class LocalizationProviderExtensions
{
    /// <summary>
    /// Creates a new <typeparamref name="T"/> and fills every public string property with its translation
    /// (current UI culture, or <paramref name="culture"/> if given). Nested [LocalizedModel] properties are filled recursively.
    /// </summary>
    /// <remarks>
    /// Why not the built-in <see cref="ILocalizationProvider.Translate{T}()"/>? It only reads resource keys that start
    /// with typeof(T).FullName, so nested [LocalizedModel] properties come back null. It also has no [Display] fallback.
    /// Still the case in LocalizationProvider 9.1.0 – Program.cs prints both for comparison.
    /// </remarks>
    public static T TranslateExtended<T>(this ILocalizationProvider provider, CultureInfo culture = null) where T : class, new()
        => (T)TranslateExtended(provider, typeof(T), culture);

    private static object TranslateExtended(ILocalizationProvider provider, Type type, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (type.GetCustomAttribute<LocalizedModelAttribute>() == null)
            throw new NotSupportedException($"{type.FullName} must have [{nameof(LocalizedModelAttribute)}]");

        var model = Activator.CreateInstance(type);
        culture ??= CultureInfo.CurrentUICulture;

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetSetMethod(false) == null || p.GetGetMethod(false) == null) continue;

            if (p.PropertyType == typeof(string))
            {
                // [UseResource] redirects to a shared resource, otherwise the key is "<Namespace>.<Type>.<Property>"
                var useResource = p.GetCustomAttribute<UseResourceAttribute>();
                var resourceKey = useResource != null
                    ? $"{useResource.TargetContainer.FullName}.{useResource.PropertyName}"
                    : $"{type.FullName}.{p.Name}";

                var translation = provider.GetString(resourceKey, culture);

                // Fallback: [Display(Name = "...")] if no translation exists
                p.SetValue(model, translation ?? p.GetCustomAttribute<DisplayAttribute>()?.Name);
            }
            else if (p.PropertyType.IsClass && p.PropertyType.GetCustomAttribute<LocalizedModelAttribute>() != null)
            {
                p.SetValue(model, TranslateExtended(provider, p.PropertyType, culture));
            }
        }

        return model;
    }
}
