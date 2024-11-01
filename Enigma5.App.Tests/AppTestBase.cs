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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Enigma5.App.Tests;

public class AppTestBase
{
    protected readonly IContainer _container;

    protected readonly ILifetimeScope _scope;

    public AppTestBase()
    {
        var serviceCollection = new ServiceCollection();
        var builder = new ContainerBuilder();

        builder.RegisterType<NetworkGraph>().InstancePerLifetimeScope();
        builder.RegisterType<TestCertificateManager>().As<ICertificateManager>().SingleInstance();
        builder.RegisterVertex();
        builder.Register(_ => Substitute.For<IConfiguration>());
        builder.Register(c => {
            var certificateManager = c.Resolve<ICertificateManager>();
            return SealProvider.Factory.CreateSigner(certificateManager.PrivateKey);
        });
        serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        builder.Populate(serviceCollection);
        builder.RegisterType<UpdateLocalAdjacencyHandler>();
        builder.RegisterType<BroadcastHandler>();

        _container = builder.Build();
        _scope = _container.BeginLifetimeScope();
    }
}
