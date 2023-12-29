namespace Enigma5.Core;

public sealed class AddressSize : AmbientContext<AddressSize>
{
    static AddressSize()
    {
        DefaultAddressSize = (int)Native.GetDefaultAddressSize();
    }

    public int Value { get; private set; }

    public AddressSize()
    {
        Value = DefaultAddressSize;
    }

    public AddressSize(int addressSize)
    {
        Value = addressSize;
        Push(this);
    }

    public static readonly int DefaultAddressSize = 32;
}
