namespace Enigma5.Core;

public sealed class PKeyContext : AmbientContext<PKeyContext>
{
    public int PKeySize { get; private set; }

    public PKeyContext()
    {
        PKeySize = DefaultPKeySize;
    }

    public PKeyContext(int pKeySize)
    {
        PKeySize = pKeySize;
        Push(this);
    }

    public static readonly int DefaultPKeySize = 2048;
}
