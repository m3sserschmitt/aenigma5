using System.Security.Cryptography;
using System.Text;

namespace Enigma5.Crypto;

public static class HashProvider
{
    public static string ToHex(byte[] data)
    => BitConverter.ToString(data).Replace("-", string.Empty).ToLower();

    public static byte[] Sha256(byte[] data) => SHA256.HashData(data);

    public static byte[] FromHexString(string hexString)
    {
        if (hexString.Length % 2 != 0)
            throw new ArgumentException("Invalid hexadecimal string.");

        int byteCount = hexString.Length / 2;
        byte[] byteArray = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            string byteString = hexString.Substring(i * 2, 2);
            byteArray[i] = Convert.ToByte(byteString, 16);
        }

        return byteArray;
    }

    public static string Sha256Hex(byte[] data)
    => ToHex(Sha256(data));

    public static string Sha256Hex(string data)
    => Sha256Hex(Encoding.UTF8.GetBytes(data));
}
