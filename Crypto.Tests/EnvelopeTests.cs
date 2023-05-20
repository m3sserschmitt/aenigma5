using Crypto.DataProviders;
using Crypto;
using Xunit;

public class EnvelopeTests
{
    [Fact]
    public void Seal_Unseal_Success()
    {
        // Arrange
        using var seal = Envelope.Factory.CreateSeal(PKey.PublicKey);
        using var unseal = Envelope.Factory.CreateUnseal(PKey.PrivateKey, PKey.Passphrase);

        byte[] plaintext = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        
        var ciphertext = seal.Seal(plaintext);
        var decrypted = unseal.Unseal(ciphertext!);
        
        Assert.NotNull(ciphertext);
        Assert.NotNull(decrypted);
        Assert.Equal(plaintext, decrypted);
    }
}
