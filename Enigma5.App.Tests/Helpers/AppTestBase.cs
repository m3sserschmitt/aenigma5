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

namespace Enigma5.App.Tests.Helpers;

public class AppTestBase
{
    protected readonly IContainer _container;

    protected readonly IMediator _mediator;

    protected readonly ICertificateManager _certificateManager;

    protected readonly IConfiguration _configuration;
    
    protected readonly NetworkGraph _graph;

    protected readonly EnigmaDbContext _dbContext;

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
        _dataSeeder = new DataSeeder(_dbContext);
        _dataSeeder.Seed();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ICertificateManager, TestCertificateManager>();

        services.AddScoped<NetworkGraph>();

        services.AddTransient(provider =>
        {
            var certificateManager = provider.GetRequiredService<ICertificateManager>();
            return SealProvider.Factory.CreateSigner(certificateManager.PrivateKey);
        });
        services.AddTransient<UpdateLocalAdjacencyHandler>();
        services.AddTransient<BroadcastHandler>();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        services.SetupMediatR();
        
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
            context.Database.EnsureCreated();

            return context;
        }).As<EnigmaDbContext>().InstancePerLifetimeScope();
        builder.RegisterVertex();
        builder.SetupConfiguration();
        builder.Populate(services);
    }
}
