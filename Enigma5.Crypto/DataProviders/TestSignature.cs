using System.Text;

namespace Enigma5.Crypto.DataProviders;

public class TestSignature
{
    public bool IsValid { get; set; }

    private byte[] Signature { get; set; }

    private TestSignature(bool isValid)
    {
        IsValid = isValid;

        var data = new byte[32];
        new Random().NextBytes(data);

        using var signature = Envelope.Factory.CreateSignature(Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Passphrase);
        Signature = signature.Sign(data) ?? new byte[1];

        if (!isValid && Signature.Length > 1)
        {
            Signature[data.Length + 10] ^= 255;
        }
    }

    public static implicit operator byte[](TestSignature signature)
    {
        return signature.Signature;
    }

    public static TestSignature CreateValidSignature()
    {
        return new TestSignature(true);
    }

    public static TestSignature CreateInvalidSignature()
    {
        return new TestSignature(false);        
    }
}
