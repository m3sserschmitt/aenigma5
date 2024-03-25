namespace Enigma5.App.Security.Contracts;

public interface ICertificateManager
{
    public string PublicKey { get; }

    public byte[] PrivateKey { get; }

    public string Address { get; }
}
