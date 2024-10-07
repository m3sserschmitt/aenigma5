namespace Enigma5.Security.Contracts;

public interface IKeysReader
{
    public byte[] PrivateKey { get; }

    public string PublicKey { get; }
}
