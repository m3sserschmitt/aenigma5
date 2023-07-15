namespace Enigma5.Crypto.Contracts;

public interface IEnvelopeSign : IDisposable
{
    byte[]? Sign(byte[] plaintext);
}
