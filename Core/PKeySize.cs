namespace Enigma5.Core;

public sealed class PKeySize : AmbientContext<PKeySize>
{
    static PKeySize()
    {
        DefaultPKeySize = (int)Native.GetDefaultPKeySize();
    }

    public int Value { get; private set; }

    public PKeySize()
    {
        Value = DefaultPKeySize;
    }

    public PKeySize(int pKeySize)
    {
        Value = pKeySize;
        Push(this);
    }

    public static readonly int DefaultPKeySize;
}
