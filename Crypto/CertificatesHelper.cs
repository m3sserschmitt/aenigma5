using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Enigma5.Crypto;

public static class CertificateHelper
{
    public static byte[] GetAddressFromPublicKey(string publicKey)
    {
        string[] lines = publicKey.Split('\n').Where(l => l.Length != 0).ToArray();

        StringBuilder base64ContentBuilder = new();
        for (int i = 1; i < lines.Length - 1; i++)
        {
            base64ContentBuilder.Append(lines[i].Trim());
        }

        return HashProvider.Sha256(Convert.FromBase64String(base64ContentBuilder.ToString()));
    }

    public static string GetHexAddressFromPublicKey(string publicKey)
    {
        var hash = GetAddressFromPublicKey(publicKey);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }
}
