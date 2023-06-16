using Enigma5.Core;
using Enigma5.Message.Tests.TestData;
using Enigma5.Crypto.DataProviders;
using Xunit;

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
        using (new AddressContext(nextAddress.Length))
        {

            // Act
            Action action = () => OnionBuilder
                .Create()
                .SetMessageContent(content)
                .SetNextAddress(nextAddress)
                .Seal(PKey.PublicKey1)
                .Build();

            // Assert
            var exception = Record.Exception(action);

            Assert.Null(exception);
        }
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
        using (new AddressContext(nextAddress.Length))
        {
            // Act
            var onion = OnionBuilder
                .Create()
                .SetMessageContent(content)
                .SetNextAddress(nextAddress)
                .Seal(PKey.PublicKey1)
                .Build();

            // Assert
            Assert.Equal(expectedEncodedSize, new byte[] { onion.Content[0], onion.Content[1] });
            Assert.Equal(expectedTotalSize, onion.Content.Length);
        }
    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldProduceDifferentCiphertextForTheSameContent(
        byte[] content,
        byte[] nextAddress,
        params object[] _)
    {
        // Arrange
        using (new AddressContext(nextAddress.Length))
        {
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
            Assert.NotEqual(onion1.Content, onion2.Content);
        }
    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldThrowExceptionForExceededContentSize(
        byte[] content,
        byte[] nextAddress,
        params object[] _)
    {
        // Arrange
        using (new AddressContext(nextAddress.Length))
        {
            // Act
            Action action = () => OnionBuilder
                .Create()
                .SetMessageContent(OnionBuilderTestData.GenerateBytes(ushort.MaxValue + 1))
                .SetNextAddress(nextAddress)
                .Seal(PKey.PublicKey1)
                .Build();

            // Assert
            var exception = Assert.Throws<ArgumentException>(action);
            Assert.Equal(
                $"Maximum size for content exceeded.",
                exception.Message);
        }
    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldThrowExceptionWhenForWrongAddressSize(
        byte[] content,
        byte[] nextAddress,
        params object[] _)
    {
        // Arrange
        using (new AddressContext(nextAddress.Length * 2))
        {
            // Act
            Action action = () => OnionBuilder
                .Create()
                .SetMessageContent(content)
                .SetNextAddress(nextAddress)
                .Seal(PKey.PublicKey1)
                .Build();

            // Assert
            var exception = Assert.Throws<ArgumentException>(action);
            Assert.Equal(
                $"Destination address length should be exactly {AddressContext.Current.AddressSize} bytes long.",
                exception.Message);
        }
    }

    [Theory]
    [ClassData(typeof(OnionBuilderTestData))]
    public void OnionBuilder_ShouldThrowExceptionForInvalidEncryptionKey(
        byte[] content,
        byte[] nextAddress,
        params object[] _)
    {
        // Arrange
        using (new AddressContext(nextAddress.Length))
        {

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
                "Message encryption failed.",
                exception.Message);
        }
    }
}
