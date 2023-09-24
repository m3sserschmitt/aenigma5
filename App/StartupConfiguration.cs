using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Hubs.Queues;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Security;
using Enigma5.Crypto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma5.App;

public class StartupConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR()
        .AddHubOptions<RoutingHub>(
            options =>
                {
                    options.AddFilter<ClientDisconnectFilter>();
                    options.AddFilter<OnionParsingFilter>();
                    options.AddFilter<OnionRoutingFilter>();
                });

        services.AddSingleton(typeof(ConnectionsMapper));
        services.AddSingleton(typeof(SessionManager));
        
#if DEBUG
        services.AddSingleton(_ => CertificateManager.CreateTestingManager());
#endif
        services.AddSingleton<OnionParsingFilter>();
        services.AddSingleton<OnionRoutingFilter>();
        services.AddSingleton<OnionQueue>();
    }

    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<RoutingHub>("/OnionRouting");
            
            endpoints.MapGet("/ServerInfo", (CertificateManager certificateManager) => {
                return Results.Ok(new {
                    certificateManager.PublicKey,
                    Address = CertificateHelper.GetHexAddressFromPublicKey(certificateManager.PublicKey)
                });
            });
        });
    }
}
