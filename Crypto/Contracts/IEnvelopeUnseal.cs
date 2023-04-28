namespace Crypto.Contracts;

public interface IEnvelopeUnseal : IDisposable
{
    byte[]? Unseal(byte[] ciphertext);
}
