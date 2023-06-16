using Enigma5.App.Contracts;
using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;

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
                    options.AddFilter<CertificateValidationFilter>();
                    options.AddFilter<OnionParserFilter>();
                    options.AddFilter<OnionRouterFilter>();
                });

        services.AddSingleton(typeof(IConnectionsMapper), typeof(ConnectionsMapper));
        services.AddSingleton<CertificateManager>(_ => new CertificateManager("./App/public.pem", "./App/private.pem"));

        services.AddSingleton<OnionParserFilter>();
        services.AddSingleton<CertificateValidationFilter>();
        services.AddSingleton<OnionRouterFilter>();
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
