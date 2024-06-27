namespace Enigma5.Crypto.DataProviders;

public class TestEnvelope
{
    public byte[] ExpectedPlaintext { get; set; }

    private byte[] SealedData { get; set; }

    private TestEnvelope()
    {
        ExpectedPlaintext = new byte[32];
        new Random().NextBytes(ExpectedPlaintext);

        using (var seal = Envelope.Factory.CreateSeal(PKey.PublicKey1))
        {
            SealedData = seal.Seal(ExpectedPlaintext) ?? new byte[1];
        }
    }

    public static implicit operator byte[](TestEnvelope envelope)
    {
        return envelope.SealedData;
    }

    public static TestEnvelope Create()
    {
        return new TestEnvelope();
    }
}
