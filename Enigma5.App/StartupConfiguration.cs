/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Extensions;
using Enigma5.App.Hangfire;
using Enigma5.App.Data;
using Enigma5.Security.Contracts;
using Enigma5.App.Common.Extensions;
using Enigma5.Security;
using Hangfire;
using Enigma5.Structures;
using System.Diagnostics.CodeAnalysis;
using Enigma5.App.Hubs.Sessions.Contracts;
using Microsoft.AspNetCore.Mvc;
using Enigma5.App.Common;
using Enigma5.App.NetworkBridge;
using Enigma5.App.UI;
using System.Text.Json.Serialization;
using Enigma5.App.Middlewares;

namespace Enigma5.App;

[ExcludeFromCodeCoverage]
public class StartupConfiguration(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR().AddHubOptions<RoutingHub>(options =>
        {
            options.AddFilter<LogFilter>();
            options.AddFilter<AuthenticatedFilter>();
            options.AddFilter<BlacklistAuthorizationFilter>();
            options.AddFilter<ValidateModelFilter>();
            options.AddFilter<OnionParsingFilter>();
            options.AddFilter<OnionRoutingFilter>();
        });
        services.AddSingleton<ConnectionsMapper>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<ICertificateManager, CertificateManager>();
        services.AddSingleton<NetworkGraph>();
        services.AddSingleton<DashboardUIState>();
        services.AddSingleton<Bridge>();
        services.AddSingleton<HubConnectionsProxy>();
        services.AddTransient<OnionParser>();
        services.AddTransient<AzureClient>();
        services.AddTransient<MediatorHangfireBridge>();
        services.AddTransient<NetworkGraphValidationPolicy>();
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.SetupKeyReader(_configuration);
        services.SetupPassphraseReader(_configuration);
        services.SetupHangfire();
        services.SetupDbContext(_configuration);
        services.SetupMediatR();
        services.AddAntiforgery();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    }

    public static void Configure(IApplicationBuilder app, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        app.UseRouting();
        app.UseAntiforgery();
        app.UseStaticFiles();
        app.UseMiddleware<HttpBlacklistAuthorizationMiddleware>();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorComponents<UI.App>().AddInteractiveServerRenderMode();
            endpoints.MapHub<RoutingHub>(Constants.OnionRoutingEndpoint);
            endpoints.MapGet(Constants.InfoEndpoint, Api.GetInfo);
            endpoints.MapPost(Constants.ShareEndpoint, Api.PostShare)
                .WithMetadata(new RequestSizeLimitAttribute(Constants.MaxSharedDataSize));
            endpoints.MapGet(Constants.ShareEndpoint, Api.GetShare);
            endpoints.MapPut(Constants.IncrementSharedDataAccessCountEndpoint, Api.IncrementSharedDataAccessCount);
            endpoints.MapGet(Constants.VerticesEndpoint, Api.GetVertices);
            endpoints.MapGet(Constants.VertexEndpoint, Api.GetVertex);
            endpoints.MapGet(Constants.LocalVertexEndpoint, Api.GetLocalVertex);
            endpoints.MapPost(Constants.FileEndpoint, Api.PostFile)
                .Accepts<IFormFile>("multipart/form-data")
                .WithMetadata(new IgnoreAntiforgeryTokenAttribute())
                .WithMetadata(new RequestSizeLimitAttribute(Constants.MaxSharedFileSize))
                .WithMetadata(new RequestFormLimitsAttribute
                {
                    MultipartBodyLengthLimit = Constants.MaxSharedFileSize
                }).DisableAntiforgery();
            endpoints.MapGet(Constants.FileEndpoint, Api.GetFile);
            endpoints.MapPut(Constants.IncrementFileAccessCountEndpoint, Api.IncrementFileAccessCount);
        });
        serviceProvider.UseAsHangfireActivator();
        serviceProvider.MigrateDatabase();
        serviceProvider.SetupMasterPassphrase();

        StartJobs(configuration);
    }

    private static void StartJobs(IConfiguration configuration)
    {
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            Constants.MessagesCleanupRecurringJob,
            bridge => bridge.Send(
                new CleanupMessagesCommand(configuration.GetMessageRetentionPeriod(), configuration.GetSentMessageRetentionPeriod())
            ),
            Constants.MessagesCleanupJobInterval
        );
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            Constants.SharedDataCleanupRecurringJob,
            bridge => bridge.Send(
                new CleanupSharedDataCommand(configuration.GetSharedDataRetentionPeriod())
            ),
            Constants.SharedDataCleanupJobInterval
        );
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            Constants.FilesCleanupRecurringJob,
            bridge => bridge.Send(
                new CleanupFilesCommand(configuration.GetFilesRetentionPeriod())
            ),
            Constants.FilesCleanupJobInterval
        );
    }
}
