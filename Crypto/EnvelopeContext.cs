using System.Runtime.InteropServices;

namespace Crypto;

public class EnvelopeContext : IDisposable
{
    private bool disposed = false;

    private IntPtr handle;

    private EnvelopeContext(IntPtr handle)
    {
        this.handle = handle;
    }

    ~EnvelopeContext()
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

    public static implicit operator IntPtr(EnvelopeContext envelopeContext)
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

        public static EnvelopeContext CreateAsymmetricEncryptionContext(string key)
        {
            return new EnvelopeContext(CreateAsymmetricEncryptionContext(key.ToArray()));
        }

        public static EnvelopeContext CreateAsymmetricDecryptionContext(string key, string passphrase)
        {
            return new EnvelopeContext(CreateAsymmetricDecryptionContext(key.ToArray(), passphrase.ToArray()));
        }

        public static EnvelopeContext CreateAsymmetricEncryptionContextFromFile(string path)
        {
            return new EnvelopeContext(CreateAsymmetricEncryptionContextFromFile(path.ToArray()));
        }

        public static EnvelopeContext CreateAsymmetricDecryptionContextFromFile(string path, string passphrase)
        {
            return new EnvelopeContext(CreateAsymmetricDecryptionContextFromFile(path.ToArray(), passphrase.ToArray()));
        }
    }
}
