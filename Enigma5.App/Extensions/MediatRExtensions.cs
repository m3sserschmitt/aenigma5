using Microsoft.Extensions.DependencyInjection;

namespace Enigma5.App.Extensions;

public static class MediatRExtensions
{
    public static IServiceCollection SetupMediatR(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(typeof(StartupConfiguration).Assembly);
        });

        return services;
    }
}