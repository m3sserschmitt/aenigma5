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

        containerBuilder.Register<ISetMessageContent>(c => OnionBuilder.Create()).As<ISetMessageContent>();
        containerBuilder.RegisterType<TestOnion>().As<ITestOnion>();
        containerBuilder.Register<TestOnionPeel>(c => new TestOnionPeel(c.Resolve<ITestOnion>()));

        container = containerBuilder.Build();
    }

    [Fact]
    public void OnionParser_ShouldParse()
    {
        // Arrange
        using (var onionParser = OnionParser.Factory.Create(PKey.PrivateKey2, PKey.Passphrase))
        {
            using (var scope = container.BeginLifetimeScope())
            {
                var onion = scope.Resolve<ITestOnion>();

                // Act
                var result = onionParser.Parse(onion);

                // Assert
                Assert.True(result);
                Assert.Equal(onion.ExpectedNextAddress, onionParser.NextAddress);
                Assert.Equal(onion.ExpectedContent, onionParser.Content);
            }
        }
    }

    [Fact]
    public void OnionParser_ShouldRemovePeel()
    {
        // Arrange
        using (var onionParser = OnionParser.Factory.Create(PKey.ServerPrivateKey, PKey.Passphrase))
        {
            using (var scope = container.BeginLifetimeScope())
            {
                var onion = scope.Resolve<TestOnionPeel>();

                // Act
                var result = onionParser.Parse(onion);

                // Assert
                Assert.True(result);
                Assert.Equal(onion.ExpectedNextAddress, onionParser.NextAddress);
                Assert.Equal(onion.ExpectedContent, onionParser.Content);
            }
        }
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
