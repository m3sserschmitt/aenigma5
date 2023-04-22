using System.Runtime.InteropServices;

namespace Crypto;

public static class RsaEncryptor
{
    [DllImport("cryptography")]
    private static extern IntPtr RsaEncrypt(IntPtr key, IntPtr passphrase, IntPtr data, uint datalen);

    [DllImport("cryptography")]
    private static extern IntPtr RsaDecrypt(IntPtr key, IntPtr passphrase, IntPtr data, uint datalen);

    private static int GetRsaSize(int keySizeBits)
    {
        return keySizeBits / 8;
    }

    public static int GetRsaOutputSize(int keySize)
    {
        return keySize / 8;
    }

    public static byte[] RsaEncrypt(char[] key, char[] passphrase, byte[] plaintext)
    {
        IntPtr keyPtr = Marshal.AllocHGlobal(key.Length);
        IntPtr plaintextPtr = Marshal.AllocHGlobal(plaintext.Length);
        IntPtr passphrasePtr = Marshal.AllocHGlobal(passphrase.Length);

        Marshal.Copy(key, 0, keyPtr, key.Length);
        Marshal.Copy(plaintext, 0, plaintextPtr, plaintext.Length);
        Marshal.Copy(passphrase, 0, passphrasePtr, passphrase.Length);

        IntPtr ciphertextPtr = RsaEncrypt(keyPtr, passphrasePtr, plaintextPtr, (uint)plaintext.Length);
        int cipherlen = GetRsaSize(plaintext.Length);

        byte[] ciphertext = new byte[cipherlen];
        Marshal.Copy(ciphertextPtr, ciphertext, 0, cipherlen);

        Marshal.FreeHGlobal(keyPtr);
        Marshal.FreeHGlobal(plaintextPtr);

        return ciphertext;
    }
}