using System.Text;
using Enigma5.App.Security.Contracts;
using Enigma5.Crypto;

namespace Enigma5.App.Security.DataProviders;

public class TestCertificateManager : ICertificateManager
{
    private readonly string _publicKey;

    private readonly string _privateKey;

    public string PublicKey => _publicKey;

    public byte[] PrivateKey => Encoding.UTF8.GetBytes(_privateKey);

    public string Address => CertificateHelper.GetHexAddressFromPublicKey(PublicKey);

    public TestCertificateManager()
    {
        (_publicKey, _privateKey) = KeysGenerator.GenerateKeys();
    }
}
