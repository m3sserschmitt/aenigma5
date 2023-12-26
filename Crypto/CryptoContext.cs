namespace Enigma5.Crypto;

internal sealed class CryptoContext : IDisposable
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

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
            }

            Native.FreeContext(handle);
            handle = IntPtr.Zero;

            disposed = true;
        }
    }

    public static implicit operator IntPtr(CryptoContext envelopeContext)
    {
        return envelopeContext.handle;
    }

    internal static class Factory
    {
        public static CryptoContext CreateAsymmetricEncryptionContext(string key)
        {
            return new CryptoContext(Native.CreateAsymmetricEncryptionContext(key));
        }

        public static CryptoContext CreateAsymmetricDecryptionContext(string key, string passphrase)
        {
            return new CryptoContext(Native.CreateAsymmetricDecryptionContext(key, passphrase));
        }

        public static CryptoContext CreateAsymmetricEncryptionContextFromFile(string path)
        {
            return new CryptoContext(Native.CreateAsymmetricEncryptionContextFromFile(path));
        }

        public static CryptoContext CreateAsymmetricDecryptionContextFromFile(string path, string passphrase)
        {
            return new CryptoContext(Native.CreateAsymmetricDecryptionContextFromFile(path, passphrase));
        }

        public static CryptoContext CreateSignatureContext(string key, string passphrase)
        {
            return new CryptoContext(Native.CreateSignatureContext(key, passphrase));
        }

        public static CryptoContext CreateSignatureContextFromFile(string path, string passphrase)
        {
            return new CryptoContext(Native.CreateSignatureContextFromFile(path, passphrase));
        }

        public static CryptoContext CreateSignatureVerificationContext(string key)
        {
            return new CryptoContext(Native.CreateVerificationContext(key));
        }

        public static CryptoContext CreateSignatureVerificationContextFromFile(string path)
        {
            return new CryptoContext(Native.CreateVerificationContextFromFile(path));
        }
    }
}
