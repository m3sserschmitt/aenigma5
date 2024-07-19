namespace Enigma5.Security.Contracts;

public interface IKeysReader
{
    public string PublicKeyPath { get; }

    public string PrivateKeyPath { get; }

    public bool PublicKeyFileExists { get; }

    public bool PrivateKeyFileExists { get; }

    public byte[] PrivateKey { get; }

    public string PublicKey { get; }
}
