using Enigma5.Core;
using Enigma5.Message.Tests.TestData;
using Enigma5.Crypto.DataProviders;
using Xunit;
using Enigma5.Crypto;
using FluentAssertions;

namespace Enigma5.Message.Tests;

public class OnionBuilderTests
{
    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldBuild(
        byte[] content,
        byte[] nextAddress,
        params object[] _)
    {
        // Arrange

        // Act
        Action action = () => OnionBuilder
            .Create()
            .SetMessageContent(content)
            .SetNextAddress(nextAddress)
            .Seal(PKey.PublicKey1)
            .Build();

        // Assert
        action.Should().NotThrow();
    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldProduceCorrectCiphertext(
        byte[] content,
        byte[] nextAddress,
        byte[] expectedEncodedSize,
        ushort expectedTotalSize)
    {
        // Arrange

        // Act
        var onion = OnionBuilder
            .Create()
            .SetMessageContent(content)
            .SetNextAddress(nextAddress)
            .Seal(PKey.PublicKey1)
            .Build();

        // Assert
        new byte[] { onion.Content[0], onion.Content[1] }.Should().Equal(expectedEncodedSize);
        onion.Content.Length.Should().Be(expectedTotalSize);
    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldProduceDifferentCiphertextForTheSameContent(
        byte[] content,
        byte[] nextAddress,
        byte[] expectedEncodedSize,
        ushort expectedTotalSize)
    {
        // Arrange

        // Act
        var onion1 = OnionBuilder
            .Create()
            .SetMessageContent(content)
            .SetNextAddress(nextAddress)
            .Seal(PKey.PublicKey1)
            .Build();
        var onion2 = OnionBuilder
            .Create()
            .SetMessageContent(content)
            .SetNextAddress(nextAddress)
            .Seal(PKey.PublicKey1)
            .Build();

        // Assert
        onion1.Content.Should().NotEqual(onion2.Content);
        new byte[] { onion1.Content[0], onion1.Content[1] }.Should().Equal(expectedEncodedSize);
        onion1.Content.Length.Should().Be(expectedTotalSize);
        new byte[] { onion2.Content[0], onion1.Content[1] }.Should().Equal(expectedEncodedSize);
        onion2.Content.Length.Should().Be(expectedTotalSize);
    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldThrowExceptionForExceededContentSize(
        params object[] data)
    {
        // Arrange

        // Act
        Action action = () => OnionBuilder
            .Create()
            .SetMessageContent(OnionBuilderTestData.GenerateBytes(ushort.MaxValue + 1))
            .SetNextAddress((byte[])data[1])
            .Seal(PKey.PublicKey1)
            .Build();

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(
            $"Maximum size for content exceeded.",
            exception.Message);

    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldThrowExceptionWhenForWrongAddressSize(
        byte[] content,
        params object[] _)
    {
        // Arrange

        // Act
        Action action = () => OnionBuilder
            .Create()
            .SetMessageContent(content)
            .SetNextAddress([1, 2, 3, 4, 5, 6, 7, 8])
            .Seal(PKey.PublicKey1)
            .Build();

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(
            $"Destination address length should be exactly {AddressSize.Value} bytes long.",
            exception.Message);

    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldThrowExceptionForInvalidEncryptionKey(
        byte[] content,
        byte[] nextAddress,
        params object[] _)
    {
        // Arrange

        // Act
        Action action = () => OnionBuilder
            .Create()
            .SetMessageContent(content)
            .SetNextAddress(nextAddress)
            .Seal("dGhpcyBpcyBhbiBpbnZhbGlkIHB1YmxpYyBrZXk=")
            .Build();

        // Assert
        var exception = Assert.Throws<Exception>(action);
        Assert.Equal(
            "Encryption context is null.",
            exception.Message);

    }

    [Fact]
    public void OnionBuilder_ShouldAddPeel()
    {
        // Arrange

        // Act
        Action action = () => OnionBuilder.Create()
            .SetMessageContent([1, 2, 3, 4, 5, 6, 7, 8])
            .SetNextAddress(HashProvider.FromHexString(PKey.Address2))
            .Seal(PKey.PublicKey2)
            .AddPeel()
            .SetNextAddress(HashProvider.FromHexString(PKey.Address2))
            .Seal(PKey.PublicKey1)
            .Build();

        // Assert
        var exception = Record.Exception(action);

        Assert.Null(exception);
    }

    [Fact]
    public void OnionBuilder_ShouldBuildOnion()
    {
        // Arrange
        var keys = new string[] { PKey.PublicKey2, PKey.PublicKey1 };
        var addresses = new string[] { PKey.Address2, PKey.Address1 };
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // Act
        var onion = OnionBuilder.CreateOnion(plaintext, keys, addresses);

        // Assert
        onion.Should().NotBeNullOrEmpty();
        onion!.Length.Should().Be(644);
        onion[0].Should().Be(2);
        onion[1].Should().Be(130);
    }

    [Fact]
    public void OnionBuilder_ShouldNotBuildWithInvalidEncryptionKey()
    {
        // Arrange
        var keys = new string[] { PKey.PublicKey2, "hdaoiuf027340-5thbashfipqoir-u==" };
        var addresses = new string[] { PKey.Address2, PKey.Address1 };
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // Act
        var onion = OnionBuilder.CreateOnion(plaintext, keys, addresses);

        // Assert
        onion.Should().BeNull();
    }
}
