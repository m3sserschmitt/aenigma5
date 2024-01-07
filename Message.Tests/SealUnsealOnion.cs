using Enigma5.Crypto.DataProviders;
using FluentAssertions;
using Xunit;

namespace Enigma5.Message.Tests;

public class SealUnsealOnionTests
{
    [Fact]
    public void ShouldSealAndUnseal()
    {
        // Arrange
        var keys = new string[] { PKey.PublicKey2, PKey.PublicKey1 };
        var addresses = new string[] { PKey.Address2, PKey.Address1 };
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var parser1 = OnionParser.Factory.Create(PKey.PrivateKey1, PKey.Passphrase);
        using var parser2 = OnionParser.Factory.Create(PKey.PrivateKey2, PKey.Passphrase);

        // Act
        var onion = OnionBuilder.CreateOnion(plaintext, keys, addresses);
        var firstParse = parser1.Parse(new Onion { Content = onion! });
        var secondParse = parser2.Parse(new Onion { Content = parser1.Content! });

        // Assert
        firstParse.Should().BeTrue();
        secondParse.Should().BeTrue();
        parser1.NextAddress.Should().Be(PKey.Address1);
        parser2.NextAddress.Should().Be(PKey.Address2);
        parser2.Content.Should().HaveCount(8);
        Assert.Equal(plaintext, parser2.Content);
    }
}
