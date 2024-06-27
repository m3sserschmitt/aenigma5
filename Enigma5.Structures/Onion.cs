using Enigma5.Structures.Contracts;

namespace Enigma5.Structures;

public class Onion : IOnion
{
    public Onion()
    {
        Content = [0x00];
    }

    public Onion(byte[] content)
    {
        Content = content;
    }

    public byte[] Content { get; set; }
}
