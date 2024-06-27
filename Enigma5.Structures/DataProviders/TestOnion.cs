using Enigma5.Structures.Contracts;
using Enigma5.Structures.DataProviders.Contracts;
using Enigma5.Crypto.DataProviders;
using Enigma5.Crypto;
using System.Text;

namespace Enigma5.Structures.DataProviders;

public class TestOnion : ITestOnion
{
    private IOnion onion;

    public TestOnion(ISetMessageContent builder)
    {
        ExpectedContent = Encoding.UTF8.GetBytes("Test Onion");
        ExpectedNextAddress = PKey.Address2;
        new Random().NextBytes(ExpectedContent);

        onion = builder
            .SetMessageContent(ExpectedContent)
            .SetNextAddress(HashProvider.FromHexString(ExpectedNextAddress))
            .Seal(PKey.PublicKey2)
            .Build();
    }

    public string ExpectedNextAddress { get; set; }

    public byte[] ExpectedContent { get; set; }

    public byte[] Content { get => onion.Content; set => onion.Content = value; }
}
