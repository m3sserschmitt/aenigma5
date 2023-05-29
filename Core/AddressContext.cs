namespace Enigma5.Core;

public sealed class AddressContext : AmbientContext<AddressContext>
{
    public int AddressSize { get; private set; }

    public AddressContext()
    {
        AddressSize = DefaultAddressSize;
    }

    public AddressContext(int addressSize)
    {
        AddressSize = addressSize;
        Push(this);
    }

    public static readonly int DefaultAddressSize = 32;
}
