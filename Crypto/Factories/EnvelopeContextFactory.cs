using System.Runtime.InteropServices;

namespace Crypto.Factories;

public static class EnvelopeContextFactory
{
    [DllImport("cryptography")]
    public static extern IntPtr CreateAsymmetricEncryptionContext(char[] key);

    [DllImport("cryptography")]
    public static extern IntPtr CreateAsymmetricDecryptionContext(char[] key, char[] passphrase);

    [DllImport("cryptography")]
    public static extern IntPtr CreateAsymmetricEncryptionContextFromFile(char[] path);

    [DllImport("cryptography")]
    public static extern IntPtr CreateAsymmetricDecryptionContextFromFile(char[] path, char[] passphrase);
}
