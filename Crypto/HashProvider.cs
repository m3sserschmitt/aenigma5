using System.Security.Cryptography;

namespace Enigma5.Crypto;

public static class HashProvider
{
    public static string Sha256(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(data);
            
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }

    public static byte[] ComputeSha256Hash(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(data);
        }
    }
}
