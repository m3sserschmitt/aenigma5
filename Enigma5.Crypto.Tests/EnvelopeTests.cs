using System.Text;
using Enigma5.Crypto.DataProviders;
using Xunit;

namespace Enigma5.Crypto.Tests;

public class EnvelopeTests
{
    [Fact]
    public void Envelope_ShouldSeal()
    {
        // Arrange
        using var seal = Envelope.Factory.CreateSeal(PKey.PublicKey1);
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        // Act
        var ciphertext = seal.Seal(plaintext);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.Equal(256 + 12 + 16 + plaintext.Length, ciphertext!.Length);
    }

    [Fact]
    public void Envelope_ShouldUnseal()
    {
        // Arrange
        var testEnvelope = TestEnvelope.Create();
        using var unseal = Envelope.Factory.CreateUnseal(Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Passphrase);

        // Act
        var plaintext = unseal.Unseal(testEnvelope);

        // Assert
        Assert.NotNull(plaintext);
        Assert.Equal(testEnvelope.ExpectedPlaintext, plaintext);
    }

    [Fact]
    public void Envelope_ShouldSign()
    {
        // Arrange
        using var signature = Envelope.Factory.CreateSignature(Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Passphrase);
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        // Act
        var ciphertext = signature.Sign(plaintext);

        // Assert
        Assert.NotNull(ciphertext);
        Assert.Equal(plaintext.Length + 256, ciphertext!.Length);
        Assert.Equal(plaintext, ciphertext!.Take(plaintext.Length));
    }

    [Fact]
    public void Envelope_ShouldVerifySignature()
    {
        // Arrange
        var testSignature = TestSignature.CreateValidSignature();
        using var signatureVerier = Envelope.Factory.CreateSignatureVerification(PKey.PublicKey1);

        // Act
        var valid = signatureVerier.Verify(testSignature);

        // Assert
        Assert.True(valid);
    }

    [Fact]
    public void Envelope_ShouldDetectInvalidSignature()
    {
        // Arrange
        var testSignature = TestSignature.CreateInvalidSignature();
        using var signatureVerier = Envelope.Factory.CreateSignatureVerification(PKey.PublicKey1);

        // Act
        var valid = signatureVerier.Verify(testSignature);

        // Assert
        Assert.False(valid);
    }
}
