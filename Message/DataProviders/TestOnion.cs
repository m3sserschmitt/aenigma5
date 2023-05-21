using Message.Contracts;
using Message.DataProviders.Contracts;
using Crypto.DataProviders;

namespace Message.DataProviders;

public class TestOnion : ITestOnion
{
    IOnion onion;

    public TestOnion(ISetMessageContent builder)
    {
        ExpectedContent = new byte[128];
        ExpectedNextAddress = new byte[32];
        new Random().NextBytes(ExpectedContent);
        new Random().NextBytes(ExpectedNextAddress);

        onion = builder
            .SetMessageContent(ExpectedContent)
            .SetNextAddress(ExpectedNextAddress)
            .Seal(PKey.PublicKey)
            .Build();
    }

    public byte[] ExpectedNextAddress { get; set; }

    public byte[] ExpectedContent { get; set; }

    public byte[] Content { get => onion.Content; set => throw new NotImplementedException(); }
    
}
