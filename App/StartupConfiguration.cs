using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Security;
using Enigma5.Crypto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Enigma5.App.MemoryStorage.Contracts;
using Enigma5.App.MemoryStorage;

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

        services.AddSingleton<ConnectionsMapper>();
        services.AddSingleton<SessionManager>();


        services.AddSingleton<CertificateManager>();

        services.AddSingleton<OnionParsingFilter>();
        services.AddSingleton<OnionRoutingFilter>();
        services.AddSingleton(typeof(IEphemeralCollection<OnionQueueItem>), typeof(OnionQueue));
        
        services.SetupHangfire();
    }

    public static void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<RoutingHub>("/OnionRouting");

            endpoints.MapGet("/ServerInfo", (CertificateManager certificateManager) =>
            {
                return Results.Ok(new
                {
                    certificateManager.PublicKey,
                    Address = CertificateHelper.GetHexAddressFromPublicKey(certificateManager.PublicKey)
                });
            });
        });

        serviceProvider.UseAsHangfireActivator();

        // TODO: Refactor this!!
        RecurringJob.AddOrUpdate<IEphemeralCollection<OnionQueueItem>>(
        "storage-cleanup",
        queue => queue.Cleanup(new TimeSpan(24, 0, 0)),
        Cron.Hourly);
    }
}
