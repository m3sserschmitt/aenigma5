using Enigma5.Crypto.Contracts;

namespace Enigma5.Crypto;

public sealed class Envelope :
    IDisposable,
    IEnvelopeUnseal,
    IEnvelopeSeal,
    IEnvelopeSign,
    IEnvelopeVerify
{
    private readonly SealProvider sealProvider;

    private Envelope(SealProvider sealProvider)
    {
        this.sealProvider = sealProvider;
    }

    public byte[]? Seal(byte[] plaintext) => sealProvider.Seal(plaintext);

    public byte[]? Unseal(byte[] ciphertext) => sealProvider.Unseal(ciphertext);

    public byte[]? Sign(byte[] plaintext) => sealProvider.Sign(plaintext);

    public bool Verify(byte[] ciphertext) => sealProvider.Verify(ciphertext);

    public void Dispose() => sealProvider.Dispose();

    public static class Factory
    {
        public static IEnvelopeSeal CreateSealFromFile(string path)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricEncryptionContextFromFile(path)));

        public static IEnvelopeUnseal CreateUnsealFromFile(string path, string passphrase)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricDecryptionContextFromFile(path, passphrase)));

        public static IEnvelopeSeal CreateSeal(string key)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricEncryptionContext(key)));

        public static IEnvelopeUnseal CreateUnseal(string key, string passphrase)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateAsymmetricDecryptionContext(key, passphrase)));

        public static IEnvelopeSign CreateSignature(string key, string passphrase)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateSignatureContext(key, passphrase)));
        
        public static IEnvelopeSign CreateSignatureFromFile(string path, string passphrase)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateSignatureContextFromFile(path, passphrase)));

        public static IEnvelopeVerify CreateSignatureVerification(string key)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateSignatureVerificationContext(key)));

        public static IEnvelopeVerify CreateSignatureVerificationFromFile(string path)
        => new Envelope(SealProvider.Create(EnvelopeContext.Factory.CreateSignatureVerificationContextFromFile(path)));
    }
}
