using Crypto.DataProviders;
using Xunit;

namespace Message.Tests;

public class OnionBuilderTests
{    
    [Fact]
    public void OnionBuilder_ShouldBuild()
    {
        // Arrange
        byte[] content = {1, 2, 3, 4, 5, 6, 7, 8};
        byte[] next = {1, 2, 3, 4, 5, 6, 7, 8};

        // Act
        var onion = OnionBuilder.Create().SetMessageContent(content).SetNextAddress(next).Seal(PKey.PublicKey).Build();

        Assert.Equal(302, onion.Content.Length);
        Assert.Equal(1, onion.Content[0]);
        Assert.Equal(44, onion.Content[1]);
    }

    [Fact]
    public void OnionBuilder_ShouldProduceDifferentContent()
    {
        // Arrange
        byte[] content = {1, 2, 3, 4, 5, 6, 7, 8};
        byte[] next = {1, 2, 3, 4, 5, 6, 7, 8};

        // Act
        var onion1 = OnionBuilder.Create().SetMessageContent(content).SetNextAddress(next).Seal(PKey.PublicKey).Build();
        var onion2 = OnionBuilder.Create().SetMessageContent(content).SetNextAddress(next).Seal(PKey.PublicKey).Build();

        // Assert
        Assert.NotEqual(onion1.Content, onion2.Content);
    }
}