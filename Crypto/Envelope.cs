using System.Runtime.InteropServices;
using Crypto.Contracts;

namespace Crypto;

public class Envelope : IDisposable, IEnvelopeUnseal, IEnvelopeSeal
{
    private EnvelopeContext handle;

    public int pKeySizeBits { get; private set; }

    private Envelope(EnvelopeContext handle, int pKeySizeBits)
    {
        this.handle = handle;
        this.pKeySizeBits = pKeySizeBits;
    }

    [DllImport("cryptography")]
    private static extern IntPtr RsaEncrypt(IntPtr ctx, byte[] plaintext, uint plaintextLen);

    [DllImport("cryptography")]
    private static extern IntPtr RsaDecrypt(IntPtr ctx, byte[] ciphertext, uint ciphertextLen);

    [DllImport("cryptography")]
    private static extern uint GetEnvelopeSize(uint pkeySizeBits, uint plaintextLen);

    [DllImport("cryptography")]
    private static extern uint GetOpenEnvelopeSize(uint pkeySizeBits, uint envelopeSize);

    public byte[]? Seal(byte[] plaintext)
    {
        IntPtr ciphertextPtr = RsaEncrypt(handle, plaintext, (uint)plaintext.Length);
        uint ciphertextLen = GetEnvelopeSize((uint)pKeySizeBits, (uint)plaintext.Length);
        byte[] ciphertext = new byte[ciphertextLen];

        if(ciphertextPtr == IntPtr.Zero)
        {
            return null;
        }

        Marshal.Copy(ciphertextPtr, ciphertext, 0, (int) ciphertextLen);

        return ciphertext;
    }

    public byte[]? Unseal(byte[] ciphertext)
    {
        IntPtr plaintextPtr = RsaDecrypt(handle, ciphertext, (uint)ciphertext.Length);
        uint plaintextLen = GetOpenEnvelopeSize((uint)pKeySizeBits, (uint)ciphertext.Length);
        byte[] plaintext = new byte[plaintextLen];

        if(plaintextPtr == IntPtr.Zero)
        {
            return null;
        }

        Marshal.Copy(plaintextPtr, plaintext, 0, (int) plaintextLen);

        return plaintext;
    }

    public void Dispose()
    {
        handle.Dispose();
    }

    public static class Factory 
    {
        public static IEnvelopeSeal CreateSealFromFile(string path, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContext.Factory.CreateAsymmetricEncryptionContextFromFile(path), pKeySizeBits);
        }

        public static IEnvelopeUnseal CreateUnsealFromFile(string path, string passphrase, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContext.Factory.CreateAsymmetricDecryptionContextFromFile(path, passphrase), pKeySizeBits);
        }

        public static IEnvelopeSeal CreateSeal(string key, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContext.Factory.CreateAsymmetricEncryptionContext(key), pKeySizeBits);
        }

        public static IEnvelopeUnseal CreateUnseal(string key, string passphrase, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContext.Factory.CreateAsymmetricDecryptionContext(key, passphrase), pKeySizeBits);
        }
    }
}
