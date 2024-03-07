namespace Enigma5.Crypto;

public sealed class Constants
{
    static Constants()
    {
        DefaultAddressSize = (int)Native.GetDefaultAddressSize();
        DefaultPKeySize = (int)Native.GetDefaultPKeySize();
    }

    public static int DefaultAddressSize { get; private set; }

    public static int DefaultPKeySize { get; private set; }
}
