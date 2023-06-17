using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Enigma5.Crypto;

public static class CertificateHelper
{
    public static byte[] GetAddressFromPublicKey(string publicKey)
    {
        using (var cryptoProvider = new RSACryptoServiceProvider())
        {
            cryptoProvider.ImportFromPem(publicKey);
            var publicKeyBytes = cryptoProvider.ExportRSAPublicKey();

            return HashProvider.Sha256(publicKeyBytes);
        }
    }

    public static string GetHexAddressFromPublicKey(string publicKey)
    {
        var hash = GetAddressFromPublicKey(publicKey);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }

    public static string? ValidateSelfSignedCertificate(string certificatePem)
    {
        try
        {
            var certificate = X509Certificate2.CreateFromPem(certificatePem);

            /*if(!certificate.Verify())
            {
                return null;
            }
            */

            var publicKey = certificate.GetPublicKey();

            return HashProvider.Sha256Hex(publicKey);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
