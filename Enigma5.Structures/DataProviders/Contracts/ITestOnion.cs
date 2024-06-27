using Enigma5.Structures.Contracts;

namespace Enigma5.Structures.DataProviders.Contracts;

public interface ITestOnion : IOnion
{
    public byte[] ExpectedContent { get; set; }

    public string ExpectedNextAddress { get; set; }
}
