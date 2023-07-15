namespace Enigma5.Crypto.Contracts;

public interface IEnvelopeVerify : IDisposable
{
    bool Verify(byte[] ciphertext);
}
