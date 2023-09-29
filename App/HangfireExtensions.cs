using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma5.App;

public static class HangfireExtensions
{
    public static IServiceCollection SetupHangfire(this IServiceCollection services)
    {
        services.AddHangfire(configuration =>
                {
                    configuration.UseInMemoryStorage();
                });
        services.AddHangfireServer();

        return services;
    }

    public static IGlobalConfiguration<HangfireActivator> UseAsHangfireActivator(this IServiceProvider serviceProvider)
    {
        return GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(serviceProvider));
    } 
}
