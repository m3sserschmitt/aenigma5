namespace Enigma5.App.Models;

public class Signature
{
    public Signature(string signedData, string publicKey)
    {
        SignedData = signedData;
        PublicKey = publicKey;
    }

    public string SignedData { get; set; }

    public string PublicKey { get; set; }
}
