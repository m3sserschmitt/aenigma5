using Enigma5.Message.Contracts;
using Enigma5.Message.DataProviders.Contracts;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.Message.DataProviders;

public class TestOnion : ITestOnion
{
    private IOnion onion;

    public TestOnion(ISetMessageContent builder)
    {
        ExpectedContent = new byte[128];
        ExpectedNextAddress = new byte[32];
        new Random().NextBytes(ExpectedContent);
        new Random().NextBytes(ExpectedNextAddress);

        onion = builder
            .SetMessageContent(ExpectedContent)
            .SetNextAddress(ExpectedNextAddress)
            .Seal(PKey.PublicKey1)
            .Build();
    }

    public byte[] ExpectedNextAddress { get; set; }

    public byte[] ExpectedContent { get; set; }

    public byte[] Content { get => onion.Content; set => throw new NotImplementedException(); }
}
