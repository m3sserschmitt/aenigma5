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
using Enigma5.App.Models;
using Enigma5.Crypto;
using System.Text;
using System.Text.Json;
using Enigma5.App.Security.Contracts;
using MediatR;
using Enigma5.App.Resources.Queries;

namespace Enigma5.App;

public class StartupConfiguration(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

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
        services.AddTransient<IPassphraseProvider, CommandLinePassphraseReader>();
        services.AddTransient<KeysProvider>();
        services.AddSingleton<ICertificateManager, CertificateManager>();
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

            endpoints.MapGet(Endpoints.ServerInfoEndpoint, (ICertificateManager certificateManager, NetworkGraph networkGraph) =>
            {
                var serializedGraph = JsonSerializer.Serialize(networkGraph.Vertices);
                var graphVersion = HashProvider.Sha256Hex(Encoding.UTF8.GetBytes(serializedGraph));

                return Results.Ok(new ServerInfo
                {
                    PublicKey = certificateManager.PublicKey,
                    Address = certificateManager.Address,
                    GraphVersion = graphVersion
                });
            });

            endpoints.MapGet(Endpoints.NetworkGraphEndpoint, (NetworkGraph networkGraph) =>
            {
                return Results.Ok(networkGraph.Vertices);
            });

            endpoints.MapGet(Endpoints.GraphAddressesEndpoint, (NetworkGraph networkGraph) =>
            {
                return Results.Ok(networkGraph.Addresses);
            });

            endpoints.MapPost(Endpoints.ShareEndpoint, async (SharedDataCreate sharedDataCreate, IMediator commandRouter) =>
            {
                if (!sharedDataCreate.Valid)
                {
                    return Results.BadRequest();
                }

                using var signatureVerification = Envelope.Factory.CreateSignatureVerification(sharedDataCreate.PublicKey!);

                if (signatureVerification is null)
                {
                    return Results.StatusCode(500);
                }

                var decodedSignature = Convert.FromBase64String(sharedDataCreate.SignedData!);

                if (decodedSignature is null
                || decodedSignature.Length == 0
                || !signatureVerification.Verify(decodedSignature))
                {
                    return Results.BadRequest();
                }

                var result = await commandRouter.Send(
                    new CreateShareDataCommand(
                        sharedDataCreate.SignedData!,
                        sharedDataCreate.AccessCount
                    )
                );

                if (result is null)
                {
                    return Results.StatusCode(500);
                }

                return Results.Ok(new { Tag = result });
            });

            endpoints.MapGet(Endpoints.ShareEndpoint, async (string tag, IMediator commandRouter) =>
            {
                var sharedData = await commandRouter.Send(new GetSharedDataQuery(tag));

                if (sharedData is null)
                {
                    return Results.NotFound();
                }

                await commandRouter.Send(new RemoveSharedDataCommand(sharedData.Tag));

                return Results.Ok(new { sharedData.Tag, sharedData.Data });
            });
        });

        serviceProvider.UseAsHangfireActivator();

        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            "pending-messages-cleanup",
            bridge => bridge.Send(new CleanupMessagesCommand(new TimeSpan(24, 0, 0), true)),
            "*/15 * * * *"
        );
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            "shared-data-cleanup",
            bridge => bridge.Send(new CleanupSharedDataCommand(new TimeSpan(0, 15, 0))),
            "*/5 * * * *"
        );
    }
}
