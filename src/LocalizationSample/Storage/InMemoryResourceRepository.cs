using System.Collections.Concurrent;
using System.Globalization;
using DbLocalizationProvider;
using DbLocalizationProvider.Abstractions;
using DbLocalizationProvider.Sync;

namespace LocalizationSample.Storage;

/// <summary>
/// Minimal in-memory storage so the sample runs without SQL Server.
/// In a real app replace <c>ctx.UseInMemoryStorage()</c> with <c>ctx.UseSqlServer(connectionString)</c>
/// (package LocalizationProvider.Storage.SqlServer).
/// </summary>
public class InMemoryResourceRepository : IResourceRepository
{
    private static readonly ConcurrentDictionary<string, LocalizationResource> Store = new();
    private static int _id;

    public IEnumerable<LocalizationResource> GetAll() => Store.Values.ToList();

    public LocalizationResource GetByKey(string resourceKey) => Store.GetValueOrDefault(resourceKey);

    public IEnumerable<CultureInfo> GetAvailableLanguages(bool includeInvariant) =>
        Store.Values.SelectMany(r => r.Translations)
            .Select(t => t.Language)
            .Distinct()
            .Where(l => includeInvariant || l != string.Empty)
            .Select(l => new CultureInfo(l))
            .ToList();

    public void InsertResource(LocalizationResource resource)
    {
        resource.Id = Interlocked.Increment(ref _id);
        Store[resource.ResourceKey] = resource;
    }

    public void UpdateResource(LocalizationResource resource) => Store[resource.ResourceKey] = resource;

    public void AddTranslation(LocalizationResource resource, LocalizationResourceTranslation translation)
    {
        var stored = Store[resource.ResourceKey];
        lock (stored)
        {
            stored.Translations.RemoveAll(t => t.Language == translation.Language);
            stored.Translations.Add(translation);
        }
    }

    public void UpdateTranslation(LocalizationResource resource, LocalizationResourceTranslation translation)
        => AddTranslation(resource, translation);

    public void DeleteTranslation(LocalizationResource resource, LocalizationResourceTranslation translation)
        => Store[resource.ResourceKey].Translations.RemoveAll(t => t.Language == translation.Language);

    public void DeleteResource(LocalizationResource resource) => Store.TryRemove(resource.ResourceKey, out _);

    public void DeleteResources(IEnumerable<LocalizationResource> resources)
    {
        foreach (var resource in resources) DeleteResource(resource);
    }

    public void DeleteAllResources() => Store.Clear();

    public void ResetSyncStatus()
    {
        foreach (var resource in Store.Values) resource.FromCode = false;
    }

    /// <summary>
    /// Called on startup with everything the type scanners found:
    /// first for [LocalizedResource] classes (<see cref="SyncSource.Resources"/>), then for [LocalizedModel] classes (<see cref="SyncSource.Models"/>).
    /// </summary>
    public void RegisterDiscoveredResources(
        ICollection<DiscoveredResource> discoveredResources,
        Dictionary<string, LocalizationResource> allResources,
        bool flexibleRefactoringMode,
        SyncSource source)
    {
        foreach (var discovered in discoveredResources)
        {
            var resource = GetByKey(discovered.Key);
            if (resource == null)
            {
                resource = new LocalizationResource(discovered.Key, enableInvariantCultureFallback: true)
                {
                    Author = "type-scanner",
                    FromCode = true,
                    IsModified = false,
                    IsHidden = discovered.IsHidden,
                    ModificationDate = DateTime.UtcNow
                };
                InsertResource(resource);
            }

            // Like the SQL implementation: code translations win unless an editor has modified the resource
            if (resource.IsModified == true) continue;

            foreach (var translation in discovered.Translations)
            {
                AddTranslation(resource, new LocalizationResourceTranslation
                {
                    ResourceId = resource.Id,
                    Language = translation.Culture,
                    Value = translation.Translation,
                    ModificationDate = DateTime.UtcNow
                });
            }
        }
    }
}

/// <summary>Storage "schema" handler – nothing to create for an in-memory store.</summary>
public class NoOpSchemaUpdater : ICommandHandler<UpdateSchema.Command>
{
    public void Execute(UpdateSchema.Command command) { }
}

public static class InMemoryStorageExtensions
{
    public static ConfigurationContext UseInMemoryStorage(this ConfigurationContext ctx)
    {
        ctx.TypeFactory.AddTransient<IResourceRepository, InMemoryResourceRepository>();
        ctx.TypeFactory.ForCommand<UpdateSchema.Command>().SetHandler<NoOpSchemaUpdater>();
        return ctx;
    }
}
