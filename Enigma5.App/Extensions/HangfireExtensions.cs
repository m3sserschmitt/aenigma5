using Enigma5.App.Hangfire;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Enigma5.App.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection SetupHangfire(this IServiceCollection services)
    {
        services.AddHangfire(configuration =>
                {
                    configuration.UseInMemoryStorage();
                    configuration.UseSerializerSettings(new JsonSerializerSettings {
                        TypeNameHandling = TypeNameHandling.All
                    });
                });
        services.AddHangfireServer();

        return services;
    }

    public static IGlobalConfiguration<HangfireActivator> UseAsHangfireActivator(this IServiceProvider serviceProvider)
    {
        return GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(serviceProvider));
    } 
}
