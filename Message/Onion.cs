using Enigma5.Message.Contracts;

namespace Enigma5.Message;

public class Onion : IOnion
{
    public Onion()
    {
        Content = new byte[1] { 0x00 };
    }

    public byte[] Content { get; set; }
}
