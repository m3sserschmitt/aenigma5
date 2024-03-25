namespace Enigma5.Crypto;

public sealed class Constants
{
    static Constants()
    {
        DefaultAddressSize = (int)Native.GetDefaultAddressSize();
        DefaultPKeySize = (int)Native.GetDefaultPKeySize();
        KernelKeyMaxSize = (int)Native.GetKernelKeyMaxSize();
    }

    public static int DefaultAddressSize { get; private set; }

    public static int DefaultPKeySize { get; private set; }

    public static int KernelKeyMaxSize { get; private set; }
}
