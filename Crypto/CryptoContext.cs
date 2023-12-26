using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public class CryptoContext : IDisposable
{
    private bool disposed = false;

    private IntPtr handle;

    private CryptoContext(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Encryption context is null.");
        }

        this.handle = handle;
    }

    ~CryptoContext()
    {
        Dispose(false);
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
            }

            FreeContext(handle);
            handle = IntPtr.Zero;

            disposed = true;
        }
    }

    [DllImport("cryptography")]
    private static extern void FreeContext(IntPtr ctx);

    public static implicit operator IntPtr(CryptoContext envelopeContext)
    {
        return envelopeContext.handle;
    }

    public static class Factory
    {
        [DllImport("cryptography")]
        private static extern IntPtr CreateAsymmetricEncryptionContext(char[] key);

        [DllImport("cryptography")]
        private static extern IntPtr CreateAsymmetricDecryptionContext(char[] key, char[] passphrase);

        [DllImport("cryptography")]
        private static extern IntPtr CreateAsymmetricEncryptionContextFromFile(char[] path);

        [DllImport("cryptography")]
        private static extern IntPtr CreateAsymmetricDecryptionContextFromFile(char[] path, char[] passphrase);

        [DllImport("cryptography")]
        private static extern IntPtr CreateSignatureContext(char[] key, char[] passphrase);

        [DllImport("cryptography")]
        private static extern IntPtr CreateVerificationContext(char[] key);

        [DllImport("cryptography")]
        private static extern IntPtr CreateSignatureContextFromFile(char[] path, char[] passphrase);

        [DllImport("cryptography")]
        private static extern IntPtr CreateVerificationContextFromFile(char[] path);

        public static CryptoContext CreateAsymmetricEncryptionContext(string key)
        {
            return new CryptoContext(CreateAsymmetricEncryptionContext(key.ToArray()));
        }

        public static CryptoContext CreateAsymmetricDecryptionContext(string key, string passphrase)
        {
            return new CryptoContext(CreateAsymmetricDecryptionContext(key.ToArray(), passphrase.ToArray()));
        }

        public static CryptoContext CreateAsymmetricEncryptionContextFromFile(string path)
        {
            return new CryptoContext(CreateAsymmetricEncryptionContextFromFile(path.ToArray()));
        }

        public static CryptoContext CreateAsymmetricDecryptionContextFromFile(string path, string passphrase)
        {
            return new CryptoContext(CreateAsymmetricDecryptionContextFromFile(path.ToArray(), passphrase.ToArray()));
        }

        public static CryptoContext CreateSignatureContext(string key, string passphrase)
        {
            return new CryptoContext(CreateSignatureContext(key.ToArray(), passphrase.ToArray()));
        }

        public static CryptoContext CreateSignatureContextFromFile(string path, string passphrase)
        {
            return new CryptoContext(CreateSignatureContextFromFile(path.ToArray(), passphrase.ToArray()));
        }

        public static CryptoContext CreateSignatureVerificationContext(string key)
        {
            return new CryptoContext(CreateVerificationContext(key.ToArray()));
        }

        public static CryptoContext CreateSignatureVerificationContextFromFile(string path)
        {
            return new CryptoContext(CreateVerificationContextFromFile(path.ToArray()));
        }
    }
}
