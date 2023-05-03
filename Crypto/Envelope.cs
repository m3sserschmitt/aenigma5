using Crypto.Contracts;

namespace Crypto;

public sealed class Envelope : IDisposable, IEnvelopeUnseal, IEnvelopeSeal
{
    private SealProvider sealProvider;

    private Envelope(SealProvider sealProvider)
    {
        this.sealProvider = sealProvider;
    }

    public byte[]? Seal(byte[] plaintext) => sealProvider.Seal(plaintext);
    
    public byte[]? Unseal(byte[] ciphertext) => sealProvider.Unseal(ciphertext);

    public void Dispose() => sealProvider.Dispose();

    public static class Factory 
    {
        public static IEnvelopeSeal CreateSealFromFile(string path)
        {
            return new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricEncryptionContextFromFile(path)));
        }

        public static IEnvelopeUnseal CreateUnsealFromFile(string path, string passphrase)
        {
            return new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricDecryptionContextFromFile(path, passphrase)));
        }

        public static IEnvelopeSeal CreateSeal(string key)
        {
            return new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricEncryptionContext(key)));
        }

        public static IEnvelopeUnseal CreateUnseal(string key, string passphrase)
        {
            return new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricDecryptionContext(key, passphrase)));
        }
    }
}
