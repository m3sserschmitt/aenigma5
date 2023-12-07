using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Extensions;
using Enigma5.App.Hangfire;
using Enigma5.App.Data;
using Enigma5.App.Common.Constants;

namespace Enigma5.App;

public class StartupConfiguration
{
    private readonly IConfiguration _configuration;

    public StartupConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR()
        .AddHubOptions<RoutingHub>(
            options =>
                {
                    options.AddFilter<OnionParsingFilter>();
                    options.AddFilter<OnionRoutingFilter>();
                });

        services.AddSingleton<ConnectionsMapper>();
        services.AddSingleton<SessionManager>();
        services.AddSingleton<CertificateManager>();
        services.AddSingleton<NetworkGraph>();
        services.AddTransient<MediatorHangfireBridge>();

        services.SetupHangfire();
        services.SetupDbContext(_configuration);
        services.SetupMediatR();
        services.AddAutoMapper(typeof(MappingProfile));
    }

    public static void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<RoutingHub>(Endpoints.OnionRoutingEndpoint);

            endpoints.MapGet(Endpoints.ServerInfoEndpoint, (CertificateManager certificateManager) =>
            {
                return Results.Ok(new
                {
                    certificateManager.PublicKey,
                    certificateManager.Address
                });
            });

            endpoints.MapGet(Endpoints.NetworkGraphEndpoint, (NetworkGraph networkGraph) =>
            {
                return Results.Ok(networkGraph.Vertices);
            });
        });

        serviceProvider.UseAsHangfireActivator();

        // TODO: Refactor this!!
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
        "storage-cleanup",
        bridge => bridge.Send(new CleanupMessagesCommand(new TimeSpan(24, 0, 0), true)),
        "*/15 * * * *");
    }
}
