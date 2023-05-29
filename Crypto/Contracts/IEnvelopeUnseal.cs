namespace Enigma5.Crypto.Contracts;

public interface IEnvelopeUnseal : IDisposable
{
    byte[]? Unseal(byte[] ciphertext);
}
