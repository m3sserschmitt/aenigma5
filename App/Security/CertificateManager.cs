using Enigma5.Crypto.DataProviders;
#if !DEBUG
using Microsoft.Extensions.Configuration;
#endif
namespace Enigma5.App.Security;

public sealed class CertificateManager
{
    public string PublicKeyFile { get; private set; }

    public string PrivateKeyFile { get; private set; }

    public string PublicKey { get; private set; }

    public string PrivateKey { get; private set; }

    public string Passphrase { get; private set; }

#if DEBUG
    public CertificateManager()
    {
        PublicKey = PKey.ServerPublicKey;
        PrivateKey = PKey.ServerPrivateKey;
        Passphrase = PKey.Passphrase;
        PublicKeyFile = string.Empty;
        PrivateKeyFile = string.Empty;
    }

#else
    public CertificateManager(IConfiguration config)
    {
        PublicKeyFile = config.GetValue<string>("PublicKeyFile");
        PrivateKeyFile = config.GetValue<string>("PrivateKeyFile");

        PublicKey = File.ReadAllText(PublicKeyFile);
        PrivateKey = File.ReadAllText(PrivateKeyFile);
        // TODO: implement key retrieval functionality!!
    }
#endif
}
