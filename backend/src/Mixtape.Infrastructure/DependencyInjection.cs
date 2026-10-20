using Microsoft.Extensions.DependencyInjection;
using Mixtape.Application.Network;
using Mixtape.Application.Rooms;
using Mixtape.Infrastructure.Network;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Infrastructure;

public static class DependencyInjection
{
    // Registrerar implementationerna av Application-lagrets interface.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IGameRoomRepository, InMemoryGameRoomRepository>();
        services.AddSingleton<IJoinAddressProvider, JoinAddressProvider>();
        return services;
    }
}
