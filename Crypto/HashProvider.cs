using System.Security.Cryptography;

namespace Enigma5.Crypto;

public static class HashProvider
{
    public static string Sha256Hex(byte[] data)
    {
        var hash = Sha256(data);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }

    public static byte[] Sha256(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(data);
        }
    }
}
