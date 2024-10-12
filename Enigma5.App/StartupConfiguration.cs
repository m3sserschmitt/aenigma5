/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Hubs.Sessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
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
using Enigma5.Security.Contracts;
using MediatR;
using Enigma5.App.Resources.Queries;
using Enigma5.App.Common.Extensions;
using Enigma5.Security;
using Hangfire;

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
                    options.AddFilter<LogFilter>();
                    options.AddFilter<AuthorizedServiceOnlyFilter>();
                    options.AddFilter<ValidateModelFilter>();
                    options.AddFilter<OnionParsingFilter>();
                    options.AddFilter<OnionRoutingFilter>();
                });

        services.AddSingleton<ConnectionsMapper>();
        services.AddSingleton<SessionManager>();
        services.AddSingleton<ICertificateManager, CertificateManager>();
        services.AddSingleton<NetworkGraph>();
        
        services.AddTransient(typeof(IKeysReader), _configuration.UseAzureVaultForKeys() ? typeof(AzureKeysReader) : typeof(KeysReader));
        services.AddTransient(typeof(IPassphraseProvider), _configuration.UseAzureVaultForPassphrase() ? typeof(AzurePassphraseReader) : typeof(CommandLinePassphraseReader));
        services.AddTransient<AzureClient>();
        services.AddTransient<MediatorHangfireBridge>();

        services.SetupHangfire();
        services.SetupDbContext(_configuration);
        services.SetupMediatR();

        services.BuildServiceProvider();
    }

    public static void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<RoutingHub>(Endpoints.OnionRoutingEndpoint);

            endpoints.MapGet(Endpoints.ServerInfoEndpoint, (ICertificateManager certificateManager, NetworkGraph networkGraph) =>
            {
                var serializedGraph = JsonSerializer.Serialize(networkGraph.Graph);
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
                return Results.Ok(networkGraph.Graph);
            });

#if DEBUG
            endpoints.MapGet(Endpoints.VerticesEndpoint, (NetworkGraph networkGraph) =>
            {
                return Results.Ok(networkGraph.Vertices);
            });
#endif

            endpoints.MapPost(Endpoints.ShareEndpoint, async (SharedDataCreate sharedDataCreate, IMediator commandRouter, IConfiguration configuration) =>
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

                var resourceUrl = $"{(configuration.GetHostname() ?? "").Trim('/')}/{Endpoints.ShareEndpoint}?Tag={result}";

                return Results.Ok(new
                {
                    Tag = result,
                    ResourceUrl = resourceUrl,
                    ValidUntil = DateTimeOffset.Now + DataPersistencePeriod.SharedDataPersistancePeriod
                }
                );
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
            bridge => bridge.Send(
                new CleanupMessagesCommand(DataPersistencePeriod.PendingMessagePersistancePeriod, true)
            ),
            "*/15 * * * *"
        );
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            "shared-data-cleanup",
            bridge => bridge.Send(
                new CleanupSharedDataCommand(DataPersistencePeriod.SharedDataPersistancePeriod)
            ),
            "*/5 * * * *"
        );
    }
}
