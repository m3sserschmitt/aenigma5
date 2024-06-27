using Autofac;
using Enigma5.Structures.Contracts;
using Enigma5.Structures.DataProviders.Contracts;
using Enigma5.Structures.DataProviders;
using Enigma5.Crypto.DataProviders;
using Xunit;
using System.Text;

namespace Enigma5.Structures.Tests;

public class OnionParserTests
{
    private readonly IContainer _container;

    public OnionParserTests()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.Register(c => OnionBuilder.Create()).As<ISetMessageContent>();
        containerBuilder.RegisterType<TestOnion>().As<ITestOnion>();
        containerBuilder.Register(c => new TestOnionPeel(c.Resolve<ITestOnion>()));

        _container = containerBuilder.Build();
    }

    [Fact]
    public void OnionParser_ShouldParse()
    {
        // Arrange
        using var onionParser = OnionParser.Factory.Create(Encoding.UTF8.GetBytes(PKey.PrivateKey2), PKey.Passphrase);
        using var scope = _container.BeginLifetimeScope();
        var onion = scope.Resolve<ITestOnion>();

        // Act
        var result = onionParser.Parse(onion);

        // Assert
        Assert.True(result);
        Assert.Equal(onion.ExpectedNextAddress, onionParser.NextAddress);
        Assert.Equal(onion.ExpectedContent, onionParser.Content);
    }

    [Fact]
    public void OnionParser_ShouldRemovePeel()
    {
        // Arrange
        using var onionParser = OnionParser.Factory.Create(Encoding.UTF8.GetBytes(PKey.ServerPrivateKey), PKey.Passphrase);
        using var scope = _container.BeginLifetimeScope();
        var onion = scope.Resolve<TestOnionPeel>();

        // Act
        var result = onionParser.Parse(onion);

        // Assert
        Assert.True(result);
        Assert.Equal(onion.ExpectedNextAddress, onionParser.NextAddress);
        Assert.Equal(onion.ExpectedContent, onionParser.Content);
    }
}
