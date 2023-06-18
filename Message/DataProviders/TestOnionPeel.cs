using Enigma5.Message.Contracts;
using Enigma5.Crypto.DataProviders;
using Enigma5.Message.DataProviders.Contracts;
using Enigma5.Crypto;

namespace Enigma5.Message.DataProviders;

public class TestOnionPeel : ITestOnion
{
    private IOnion onion;

    public TestOnionPeel(ITestOnion testOnion)
    {
        ExpectedNextAddress = PKey.Address2;
        ExpectedContent = (byte[])testOnion.Content.Clone();

        onion = OnionBuilder
            .Create(testOnion)
            .AddPeel()
            .SetNextAddress(HashProvider.FromHexString(ExpectedNextAddress))
            .Seal(PKey.PublicKey2)
            .Build();
    }

    public string ExpectedNextAddress { get; set; }

    public byte[] ExpectedContent { get; set; }

    public byte[] Content { get => onion.Content; set => onion.Content = value; }
}
