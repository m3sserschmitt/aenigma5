public sealed class CertificateManager
{
    public string PublicKey { get; private set; }

    public string PrivateKey { get; private set; }

    [Obsolete("Unsecure; to be used for testing purposes only!")]
    public string Passphrase { get; private set; }

    public CertificateManager(string publicKeyPath, string privateKeyPath)
    {
        PublicKey = File.ReadAllText(publicKeyPath);
        PrivateKey = File.ReadAllText(privateKeyPath);
        Passphrase = "12345678";
    }
}
