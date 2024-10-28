/*
    Aenigma - Federal messaging system
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
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Extensions;
using Enigma5.App.Hangfire;
using Enigma5.App.Data;
using Enigma5.App.Common.Constants;
using Enigma5.Security.Contracts;
using Enigma5.App.Common.Extensions;
using Enigma5.Security;
using Hangfire;
using Enigma5.Crypto;
using Enigma5.Structures;

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
        services.AddTransient(provider => {
            var certificateManager = provider.GetRequiredService<ICertificateManager>();
            return SealProvider.Factory.CreateUnsealer(certificateManager.PrivateKey);
        });
        services.AddTransient(provider => {
            var certificateManager = provider.GetRequiredService<ICertificateManager>();
            return SealProvider.Factory.CreateSigner(certificateManager.PrivateKey);
        });
        services.AddTransient<OnionParser>();
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
            endpoints.MapGet(Endpoints.InfoEndpoint, Api.GetInfo);
            endpoints.MapPost(Endpoints.ShareEndpoint, Api.PostShare);
            endpoints.MapGet(Endpoints.ShareEndpoint, Api.GetShare);
            endpoints.MapGet(Endpoints.VerticesEndpoint, Api.GetVertices);
            endpoints.MapGet(Endpoints.VertexEndpoint, Api.GetVertex);
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
