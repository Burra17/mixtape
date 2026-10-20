using Microsoft.Extensions.DependencyInjection;
using Mixtape.Application.Rooms;

namespace Mixtape.Application;

public static class DependencyInjection
{
    // Registrerar Application-lagrets tjänster, alltså handlers.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<CreateRoomHandler>();
        services.AddTransient<ResumeRoomHandler>();
        services.AddTransient<JoinRoomHandler>();
        services.AddTransient<LeaveRoomHandler>();
        services.AddTransient<RemovePlayerHandler>();
        return services;
    }
}
