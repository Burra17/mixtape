using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Application.Network;
using Mixtape.Application.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Music;
using Mixtape.Infrastructure.Music.Deezer;
using Mixtape.Infrastructure.Network;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Infrastructure;

public static class DependencyInjection
{
    // Registrerar implementationerna av Application-lagrets interface.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IGameRoomRepository, InMemoryGameRoomRepository>();
        services.AddSingleton<IJoinAddressProvider, JoinAddressProvider>();
        services.AddSingleton<IConnectionRegistry, InMemoryConnectionRegistry>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<RoomCleanupService>();
        services.AddMusicCatalog(configuration);
        return services;
    }

    // Deezer bakom en cache. Hubben och handlers ser bara IMusicCatalog.
    private static void AddMusicCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Deezer:BaseUrl"] ?? throw new InvalidOperationException("Deezer:BaseUrl saknas i appsettings.");

        services.AddMemoryCache();
        services.AddHttpClient<DeezerMusicCatalog>(http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(5);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mixtape/1.0");
        });
        services.AddTransient<IMusicCatalog>(provider =>
            new CachingMusicCatalog(provider.GetRequiredService<DeezerMusicCatalog>(), provider.GetRequiredService<IMemoryCache>()));
    }
}
