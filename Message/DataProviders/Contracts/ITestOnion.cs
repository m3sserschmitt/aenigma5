using Message.Contracts;

namespace Message.DataProviders.Contracts;

public interface ITestOnion : IOnion
{
    public byte[] ExpectedContent { get; set; }

    public byte[] ExpectedNextAddress { get; set; }
}
