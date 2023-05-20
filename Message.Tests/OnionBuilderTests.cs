using Xunit;

namespace Message.Tests;

public class OnionBuilderTests
{    
    private static readonly string PUBLIC_KEY = "-----BEGIN PUBLIC KEY-----\n" +
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAt93z0JRoIKt0f+Yoy6KB\n" +
        "c3AYlN2LiA4NH3EsVtVFdPyOboEpDIKMQwuSP9Gi/+hBHgHnO8YXU/ytBygAzE93\n" +
        "o/BzMtNNgQS+FDDiuD19+65525rI+IZL+vulhvUVsUZgHmW7r0ACB8qxmQdmotLr\n" +
        "zgyRprJo1kCRQajS5ICsjWqx/w/s39k5V8XJnIYCAIcSiG9N22Z3GY3x1ewOfU15\n" +
        "Amw3lb7s6ccOccVUgrDWMqjfaVzYebFmXhyJ99+xp2YOjiIfwL/dDIy2R7chiTSr\n" +
        "uLWhUdX9FPjSpsTCu7vOq0fKitIe9yIXkcA+WZSU4AqxH3h+9eJtlG0/yiK/thkG\n" +
        "OwIDAQAB\n" +
        "-----END PUBLIC KEY-----\n";

    [Fact]
    public void OnionBuilder_ShouldBuild()
    {
        // Arrange
        byte[] content = {1, 2, 3, 4, 5, 6, 7, 8};
        byte[] next = {1, 2, 3, 4, 5, 6, 7, 8};

        // Act
        var onion = OnionBuilder.Create().SetMessageContent(content).SetNextAddress(next).Seal(PUBLIC_KEY).Build();

        Assert.Equal(302, onion.Content.Length);
        Assert.Equal(44, onion.Content[0]);
        Assert.Equal(1, onion.Content[1]);
    }

    [Fact]
    public void OnionBuilder_ShouldProduceDifferentContent()
    {
        // Arrange
        byte[] content = {1, 2, 3, 4, 5, 6, 7, 8};
        byte[] next = {1, 2, 3, 4, 5, 6, 7, 8};

        // Act
        var onion1 = OnionBuilder.Create().SetMessageContent(content).SetNextAddress(next).Seal(PUBLIC_KEY).Build();
        var onion2 = OnionBuilder.Create().SetMessageContent(content).SetNextAddress(next).Seal(PUBLIC_KEY).Build();

        // Assert
        Assert.NotEqual(onion1.Content, onion2.Content);
    }
}