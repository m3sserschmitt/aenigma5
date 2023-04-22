using System.Runtime.InteropServices;

namespace Crypto;

public class AesGcmEncryptor
{
    [DllImport("cryptography")]
    private static extern IntPtr AesGcmEncrypt(IntPtr key, IntPtr data, uint datalen);

    [DllImport("cryptography")]
    private static extern IntPtr AesGcmDecrypt(IntPtr key, IntPtr data, uint datalen);

    [DllImport("cryptography")]
    public static extern int GetAesGcmCiphertextSize(int plaintext);

    [DllImport("cryptography")]
    public static extern int GetAesGcmPlaintextSize(int plaintext);

    public static byte[] AesGcm256Encrypt(byte[] key, byte[] plaintext)
    {
        IntPtr keyPtr = Marshal.AllocHGlobal(key.Length);
        IntPtr plaintextPtr = Marshal.AllocHGlobal(plaintext.Length);

        Marshal.Copy(key, 0, keyPtr, key.Length);
        Marshal.Copy(plaintext, 0, plaintextPtr, plaintext.Length);

        IntPtr ciphertextPtr = AesGcmEncrypt(keyPtr, plaintextPtr, (uint)plaintext.Length);
        int cipherlen = GetAesGcmCiphertextSize(plaintext.Length);

        byte[] ciphertext = new byte[cipherlen];
        Marshal.Copy(ciphertextPtr, ciphertext, 0, cipherlen);

        Marshal.FreeHGlobal(keyPtr);
        Marshal.FreeHGlobal(plaintextPtr);

        return ciphertext;
    }

    public static byte[] AesGcm256Decrypt(byte[] key, byte[] ciphertext)
    {
        IntPtr keyPtr = Marshal.AllocHGlobal(key.Length);
        IntPtr ciphertextPtr = Marshal.AllocHGlobal(ciphertext.Length);

        Marshal.Copy(key, 0, keyPtr, key.Length);
        Marshal.Copy(ciphertext, 0, ciphertextPtr, ciphertext.Length);

        IntPtr plaintextPtr = AesGcmDecrypt(keyPtr, ciphertextPtr, (uint)ciphertext.Length);
        int plaintextlen = GetAesGcmPlaintextSize(ciphertext.Length);

        byte[] plaintext = new byte[plaintextlen];
        Marshal.Copy(plaintextPtr, plaintext, 0, plaintextlen);

        Marshal.FreeHGlobal(keyPtr);
        Marshal.FreeHGlobal(plaintextPtr);

        return plaintext;
    }
}
