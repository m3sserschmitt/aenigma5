using Enigma5.App.Contracts;
using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma5.App;

public class StartupConfiguration
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR()
        .AddHubOptions<RoutingHub>(
            options =>
                {
                    options.AddFilter<ClientDisconnectFilter>();
                    options.AddFilter<CertificateValidationFilter>();
                    options.AddFilter<OnionParsingFilter>();
                    options.AddFilter<OnionRoutingFilter>();
                });

        services.AddSingleton(typeof(IConnectionsMapper), typeof(ConnectionsMapper));

#if DEBUG
        services.AddSingleton<CertificateManager>(_ => CertificateManager.CreateTestingManager());
#endif
        services.AddSingleton<OnionParsingFilter>();
        services.AddSingleton<CertificateValidationFilter>();
        services.AddSingleton<OnionRoutingFilter>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<RoutingHub>("/OnionRouting");
        });
    }
}
