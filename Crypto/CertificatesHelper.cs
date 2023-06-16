using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Enigma5.Crypto;

public static class CertificateHelper
{
    /*public static X509Certificate2 InitializeCertificate(string certificatePem)
    {
        string[] lines = certificatePem.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        string[] trimmedLines = lines.Skip(1).Take(lines.Length - 2).ToArray();
        string trimmedCertificate = string.Join(string.Empty, trimmedLines);

        byte[] certificateBytes = Convert.FromBase64String(trimmedCertificate);
        var c = X509Certificate2.CreateFromPem(certificatePem);
        return new X509Certificate2(certificateBytes);
    }*/

    public static byte[] GetAddressFromPublicKey(string publicKey)
    {
        /*
        string[] pemLines = publicKey.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        pemLines = pemLines.Skip(1).Take(pemLines.Length - 2).ToArray();

        string publicKeyBase64 = string.Concat(pemLines);
        byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

        return HashProvider.ComputeSha256Hash(publicKeyBytes);
        */

        var cryptoProvider = new RSACryptoServiceProvider();
        cryptoProvider.ImportFromPem(publicKey);
        var publicKeyBytes = cryptoProvider.ExportRSAPublicKey();

        return HashProvider.ComputeSha256Hash(publicKeyBytes);
    }

    public static string? ValidateSelfSignedCertificate(string certificatePem)
    {
        try
        {
            var certificate = X509Certificate2.CreateFromPem(certificatePem);

            /*X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            if(!chain.Build(certificate))
            {
                return null;
            }*/

            var publicKey = certificate.GetPublicKey();

            return HashProvider.Sha256(publicKey);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
