using Enigma5.Message.Contracts;
using Enigma5.Crypto.DataProviders;
using Enigma5.Message.DataProviders.Contracts;

namespace Enigma5.Message.DataProviders;

public class TestOnionPeel : ITestOnionPeel
{
    private IOnion onion;

    public TestOnionPeel(ITestOnion testOnion)
    {
        ExpectedNextAddress = new byte[32];
        new Random().NextBytes(ExpectedNextAddress);

        onion = OnionBuilder.Create(testOnion).AddPeel().SetNextAddress(ExpectedNextAddress).Seal(PKey.PublicKey2).Build();
    }

    public byte[] ExpectedNextAddress { get; set; }

    public byte[] Content { get => onion.Content; set => throw new NotImplementedException(); }
}
