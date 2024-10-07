using Enigma5.Crypto.Contracts;

namespace Enigma5.Crypto;

public sealed class Envelope :
    IDisposable,
    IEnvelopeUnseal,
    IEnvelopeSeal,
    IEnvelopeSign,
    IEnvelopeVerify
{
    private readonly SealProvider _sealProvider;

    private Envelope(SealProvider sealProvider)
    {
        _sealProvider = sealProvider;
    }

    ~Envelope()
    {
        _sealProvider.Dispose();
    }

    public byte[]? Seal(byte[] plaintext) => _sealProvider.Seal(plaintext);

    public byte[]? Unseal(byte[] ciphertext) => _sealProvider.Unseal(ciphertext);

    public IntPtr UnsealOnion(byte[] onion, out int outLen)
    => _sealProvider.UnsealOnion(onion, out outLen);

    public static IntPtr SealOnion(
        byte[] plaintext,
        string[] keys,
        string[] addresses,
        out int outLen)
    => SealProvider.SealOnion(plaintext, keys, addresses, out outLen);

    public byte[]? Sign(byte[] plaintext) => _sealProvider.Sign(plaintext);

    public bool Verify(byte[] ciphertext) => _sealProvider.Verify(ciphertext);

    public static int GetEnvelopeSize(int plaintextLen)
    => (int)Native.GetEnvelopeSize((uint)plaintextLen);

    public void Dispose()
    {
        _sealProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    public static class Factory
    {
        public static IEnvelopeSeal CreateSealFromFile(string path)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricEncryptionContextFromFile(path)));

        public static IEnvelopeUnseal CreateUnsealFromFile(string path, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricDecryptionContextFromFile(path, passphrase)));

        public static IEnvelopeSeal CreateSeal(string key)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricEncryptionContext(key)));

        public static IEnvelopeUnseal CreateUnseal(byte[] key, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateAsymmetricDecryptionContext(key, passphrase)));

        public static IEnvelopeSign CreateSignature(byte[] key, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureContext(key, passphrase)));

        public static IEnvelopeSign CreateSignatureFromFile(string path, string passphrase)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureContextFromFile(path, passphrase)));

        public static IEnvelopeVerify CreateSignatureVerification(string key)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureVerificationContext(key)));

        public static IEnvelopeVerify CreateSignatureVerificationFromFile(string path)
        => new Envelope(SealProvider.Factory.Create(CryptoContext.Factory.CreateSignatureVerificationContextFromFile(path)));
    }
}