using Autofac;
using Enigma5.Message.Contracts;
using Enigma5.Message.DataProviders.Contracts;
using Enigma5.Message.DataProviders;
using Enigma5.Crypto.DataProviders;
using Xunit;

namespace Enigma5.Message.Tests;

public class OnionParserTests
{
    private IContainer container;

    public OnionParserTests()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.Register(c => { return OnionBuilder.Create(); }).As<ISetMessageContent>();
        containerBuilder.RegisterType<TestOnion>().As<ITestOnion>();
        containerBuilder.RegisterType<TestOnionPeel>().As<ITestOnionPeel>();

        container = containerBuilder.Build();
    }

    [Fact]
    public void OnionParser_ShouldParse()
    {
        // Arrange
        using var scope = container.BeginLifetimeScope();
        var onion = scope.Resolve<ITestOnion>();
        var onionParser = OnionParser.Factory.Create(PKey.PrivateKey1, PKey.Passphrase);

        // Act
        var result = onionParser.Parse(onion);

        // Assert
        Assert.True(result);
        Assert.Equal(onion.ExpectedNextAddress, onionParser.Next);
        Assert.Equal(onion.ExpectedContent, onionParser.Content);
    }

    [Fact]
    public void OnionParser_ShoulRemovePeel()
    {
        
    }

    [Theory]
    [InlineData(1, 44, 300)]
    [InlineData(10, 78, 2638)]
    [InlineData(234, 103, 60007)]
    public void OnionParser_ShouldDecodeSize(byte first, byte second, int expected)
    {
        // Arrange
        var encodedSize = new byte[] { first, second };

        // Act
        var decodedSize = OnionParser.DecodeSize(encodedSize);

        // Assert
        Assert.Equal(expected, decodedSize);
    }
}
