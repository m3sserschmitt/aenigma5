using Message.Contracts;

namespace Message;

public class Message : IMessage
{
    public byte[]? Content { get; set; }
}
