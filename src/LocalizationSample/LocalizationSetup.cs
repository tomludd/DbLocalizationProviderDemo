using DbLocalizationProvider;
using DbLocalizationProvider.AspNetCore;
using LocalizationSample.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LocalizationSample;

public static class LocalizationSetup
{
    /// <summary>
    /// Registers DbLocalizationProvider, syncs [LocalizedResource]/[LocalizedModel] classes to storage
    /// and returns the provider. Used by both Program.cs and the tests.
    /// </summary>
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

        // 2. Scan assemblies and sync translations to storage.
        //    In ASP.NET Core, app.UseDbLocalizationProvider() does exactly this.
        serviceProvider.UseDbLocalizationProvider();

        return serviceProvider.GetRequiredService<ILocalizationProvider>();
    }
}
