namespace Enigma5.Core;

public static class PKeySize
{
    static PKeySize()
    {
        Value = (int)Native.GetDefaultPKeySize();
    }

    public static int Value { get; private set; }
}
