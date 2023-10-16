namespace Enigma5.App.Security;

public sealed class CertificateManager
{
    public string PublicKey { get; private set; }

    public string PrivateKey { get; private set; }

    public CertificateManager()
    {
        (PublicKey, PrivateKey) = KeysGenerator.GenerateKeys();
    }
}
