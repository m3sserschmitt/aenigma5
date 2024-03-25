using Autofac;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Enigma5.App.Data;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Security.Contracts;
using Enigma5.App.Security.DataProviders;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Enigma5.App.Tests;

public class AppTestBase
{
    protected readonly IContainer _container;

    protected readonly ILifetimeScope _scope;

    public AppTestBase()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<NetworkGraph>();
        builder.RegisterType<TestCertificateManager>().As<ICertificateManager>().SingleInstance();
        builder.RegisterVertex();
        builder.Register(_ => Substitute.For<IConfiguration>());

        builder.RegisterType<UpdateLocalAdjacencyHandler>();
        builder.RegisterType<BroadcastHandler>();

        builder.RegisterAutoMapper(typeof(App).Assembly);

        _container = builder.Build();
        _scope = _container.BeginLifetimeScope();
    }
}
