namespace Crypto.Contracts;

public interface IEnvelopeSeal : IDisposable
{
    byte[]? Seal(byte[] plaintext);
}
