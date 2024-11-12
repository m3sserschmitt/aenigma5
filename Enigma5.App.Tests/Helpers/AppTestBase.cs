﻿/*
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

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Enigma5.App.Data;
using Enigma5.App.Resources.Handlers;
using Enigma5.Crypto;
using Enigma5.Security.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Enigma5.App.Extensions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Sessions;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Enigma5.App.Hubs.Filters;
using Enigma5.Structures;
using Enigma5.App.Hubs.Sessions.Contracts;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.App.Tests.Helpers;

[ExcludeFromCodeCoverage]
public class AppTestBase : IAsyncLifetime
{
    protected readonly IContainer _container;

    protected readonly IMediator _mediator;

    protected readonly ICertificateManager _certificateManager;

    protected readonly IConfiguration _configuration;
    
    protected readonly NetworkGraph _graph;

    protected readonly EnigmaDbContext _dbContext;

    protected readonly RoutingHub _hub;

    protected readonly ISessionManager _sessionManager;

    protected readonly ConnectionsMapper _connectionMapper;

    protected readonly DataSeeder _dataSeeder;

    public AppTestBase()
    {
        var services = new ServiceCollection();
        var builder = new ContainerBuilder();

        ConfigureServices(services);
        ConfigureContainer(builder, services);

        _container = builder.Build();

        _mediator = _container.Resolve<IMediator>();
        _certificateManager = _container.Resolve<ICertificateManager>();
        _graph = _container.Resolve<NetworkGraph>();
        _configuration = _container.Resolve<IConfiguration>();
        _dbContext = _container.Resolve<EnigmaDbContext>();
        _hub = _container.Resolve<RoutingHub>();
        _sessionManager = _container.Resolve<ISessionManager>();
        _connectionMapper = _container.Resolve<ConnectionsMapper>();
        _dataSeeder = _container.Resolve<DataSeeder>();
        ConfigureSignalRHub();
        ConfigureSessionManager();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICertificateManager, TestCertificateManager>();
        services.AddScoped(provider => Substitute.For<ISessionManager>());
        services.AddScoped<ConnectionsMapper>();
        services.AddScoped<NetworkGraph>();
        services.AddScoped<RoutingHub>();

        services.AddTransient(provider =>
        {
            var certificateManager = provider.GetRequiredService<ICertificateManager>();
            return SealProvider.Factory.CreateSigner(certificateManager.PrivateKey);
        });
        services.AddTransient(provider => {
            var certificateManager = provider.GetRequiredService<ICertificateManager>();
            return SealProvider.Factory.CreateUnsealer(certificateManager.PrivateKey);
        });
        services.AddTransient<BroadcastHandler>();
        services.AddTransient<CheckAuthorizedServiceHandler>();
        services.AddTransient<UpdateLocalAdjacencyHandler>();
        services.AddTransient<CleanupMessagesHandler>();
        services.AddTransient<CleanupSharedDataHandler>();
        services.AddTransient<AuthenticatedFilter>();
        services.AddTransient<OnionParsingFilter>();
        services.AddTransient<OnionRoutingFilter>();
        services.AddTransient<ValidateModelFilter>();
        services.AddTransient<LogFilter>();
        services.AddTransient<OnionParser>();
        services.AddTransient<DataSeeder>();
        
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        services.SetupMediatR();
    }

    private void ConfigureSignalRHub()
    {
        var hubCallerContext = Substitute.For<HubCallerContext>();
        hubCallerContext.ConnectionId.Returns("test-connection-id");
        _hub.Context = hubCallerContext;
        _hub.Clients = Substitute.For<IHubCallerClients>();
    }

    private void ConfigureSessionManager()
    {
        _sessionManager.AddPending(_hub.Context.ConnectionId).Returns("test-nonce");
        _sessionManager.Authenticate(_hub.Context.ConnectionId, PKey.PublicKey1, Arg.Any<string>()).Returns(true);
        _sessionManager.TryGetAddress(_hub.Context.ConnectionId, out Arg.Any<string?>()).Returns(args => {
            args[1] = PKey.Address1;
            return true;
        });
        _sessionManager.TryGetConnectionId(PKey.Address2, out Arg.Any<string?>()).Returns(args => {
            args[1] = "test-connection-id-2";
            return true;
        });
    }

    private static void ConfigureContainer(ContainerBuilder builder, IServiceCollection services)
    {
        builder.Register(c =>
        {
            var options = new DbContextOptionsBuilder<EnigmaDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            var context = new EnigmaDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }).As<EnigmaDbContext>().SingleInstance();
        builder.RegisterVertex();
        builder.SetupConfiguration();
        builder.Populate(services);
    }

    public async Task InitializeAsync()
    {
        await _dataSeeder.Seed();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
