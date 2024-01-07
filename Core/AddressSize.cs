namespace Enigma5.Core;

public sealed class AddressSize
{
    static AddressSize()
    {
        Value = (int)Native.GetDefaultAddressSize();
    }

    public static int Value { get; private set; }
}
