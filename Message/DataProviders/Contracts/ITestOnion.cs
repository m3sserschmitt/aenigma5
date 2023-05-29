using Enigma5.Message.Contracts;

namespace Enigma5.Message.DataProviders.Contracts;

public interface ITestOnion : IOnion
{
    public byte[] ExpectedContent { get; set; }

    public byte[] ExpectedNextAddress { get; set; }
}
