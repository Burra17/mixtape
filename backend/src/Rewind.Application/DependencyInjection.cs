using Microsoft.Extensions.DependencyInjection;

namespace Rewind.Application;

public static class DependencyInjection
{
    // Registrerar Application-lagrets tjänster, alltså handlers.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
