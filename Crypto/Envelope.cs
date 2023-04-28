using System.Runtime.InteropServices;
using Crypto.Contracts;
using Crypto.Factories;

namespace Crypto;

public class Envelope : IEnvelopeUnseal, IEnvelopeSeal
{
    private bool disposed = false;

    private IntPtr handle;

    public int pKeySizeBits { get; private set; }

    private Envelope(IntPtr handle, int pKeySizeBits)
    {
        this.handle = handle;
        this.pKeySizeBits = pKeySizeBits;
    }

    [DllImport("cryptography")]
    private static extern IntPtr RsaEncrypt(IntPtr ctx, byte[] plaintext, uint plaintextLen);

    [DllImport("cryptography")]
    private static extern IntPtr RsaDecrypt(IntPtr ctx, byte[] ciphertext, uint ciphertextLen);

    [DllImport("cryptography")]
    private static extern void FreeContext(IntPtr ctx);

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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Release managed resources here
            }

            // Release unmanaged resources here
            FreeContext(handle);

            disposed = true;
        }
    }

    ~Envelope()
    {
        Dispose(false);
    }

    public static class Factory 
    {
        public static IEnvelopeSeal CreateSealFromFile(string path, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContextFactory.CreateAsymmetricEncryptionContextFromFile(path.ToArray()), pKeySizeBits);
        }

        public static IEnvelopeUnseal CreateUnsealFromFile(string path, string passphrase, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContextFactory.CreateAsymmetricDecryptionContextFromFile(path.ToArray(), passphrase.ToArray()), pKeySizeBits);
        }

        public static IEnvelopeSeal CreateSeal(string key, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContextFactory.CreateAsymmetricEncryptionContext(key.ToArray()), pKeySizeBits);
        }

        public static IEnvelopeUnseal CreateUnseal(string key, string passphrase, int pKeySizeBits)
        {
            return new Envelope(EnvelopeContextFactory.CreateAsymmetricDecryptionContext(key.ToArray(), passphrase.ToArray()), pKeySizeBits);
        }
    }
}
