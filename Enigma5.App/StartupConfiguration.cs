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
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Enigma5.App;

[ExcludeFromCodeCoverage]
public class StartupConfiguration(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR()
        .AddHubOptions<RoutingHub>(options =>
        {
            options.AddFilter<LogFilter>();
            options.AddFilter<AuthenticatedFilter>();
            options.AddFilter<AuthorizedServiceOnlyFilter>();
            options.AddFilter<ValidateModelFilter>();
            options.AddFilter<OnionParsingFilter>();
            options.AddFilter<OnionRoutingFilter>();
        });
        services.AddSingleton<ConnectionsMapper>();
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<ICertificateManager, CertificateManager>();
        services.AddSingleton<NetworkGraph>();
        services.AddTransient<OnionParser>();
        services.AddTransient<AzureClient>();
        services.AddTransient<MediatorHangfireBridge>();
        services.SetupSigner();
        services.SetupUnsealer();
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.SetupKeyReader(_configuration);
        services.SetupPassphraseReader(_configuration);
        services.SetupHangfire();
        services.SetupDbContext(_configuration);
        services.SetupMediatR();
        services.AddAntiforgery();
    }

    public static void Configure(IApplicationBuilder app, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        app.UseRouting();
        app.UseAntiforgery();
        app.UseStaticFiles();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorComponents<UI.App>().AddInteractiveServerRenderMode();
            endpoints.MapHub<RoutingHub>(Common.Constants.OnionRoutingEndpoint);
            endpoints.MapGet(Common.Constants.InfoEndpoint, Api.GetInfo);
            endpoints.MapPost(Common.Constants.ShareEndpoint, Api.PostShare)
                .WithMetadata(new RequestSizeLimitAttribute(Common.Constants.MaxSharedDataSize));
            endpoints.MapGet(Common.Constants.ShareEndpoint, Api.GetShare);
            endpoints.MapPut(Common.Constants.IncrementSharedDataAccessCountEndpoint, Api.IncrementSharedDataAccessCount);
            endpoints.MapGet(Common.Constants.VerticesEndpoint, Api.GetVertices);
            endpoints.MapGet(Common.Constants.VertexEndpoint, Api.GetVertex);
            endpoints.MapPost(Common.Constants.FileEndpoint, Api.PostFile)
                .Accepts<IFormFile>("multipart/form-data")
                .WithMetadata(new IgnoreAntiforgeryTokenAttribute())
                .WithMetadata(new RequestSizeLimitAttribute(Common.Constants.MaxSharedFileSize))
                .WithMetadata(new RequestFormLimitsAttribute
                {
                    MultipartBodyLengthLimit = Common.Constants.MaxSharedFileSize
                }).DisableAntiforgery();
            endpoints.MapGet(Common.Constants.FileEndpoint, Api.GetFile);
            endpoints.MapPut(Common.Constants.IncrementFileAccessCountEndpoint, Api.IncrementFileAccessCount);
        });

        serviceProvider.UseAsHangfireActivator();

        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            "messages-cleanup",
            bridge => bridge.Send(
                new CleanupMessagesCommand(configuration.GetMessageRetentionPeriod(), configuration.GetSentMessageRetentionPeriod())
            ),
            "*/5 * * * *"
        );
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            "shared-data-cleanup",
            bridge => bridge.Send(
                new CleanupSharedDataCommand(configuration.GetSharedDataRetentionPeriod())
            ),
            "*/5 * * * *"
        );
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            "files-cleanup",
            bridge => bridge.Send(
                new CleanupFilesCommand(configuration.GetFilesRetentionPeriod())
            ),
            "*/5 * * * *"
        );

        using var scope = app.ApplicationServices.CreateScope();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<StartupConfiguration>>();
            logger.LogError(ex, "An error occurred while creating the database.");
            throw;
        }

        scope.ServiceProvider.GetRequiredService<IMediator>().Send(new SetMasterPassphraseCommand(null));
    }
}
