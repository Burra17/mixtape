using Microsoft.Extensions.DependencyInjection;

namespace Rewind.Infrastructure;

public static class DependencyInjection
{
    // Registrerar implementationerna av Application-lagrets interface.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
