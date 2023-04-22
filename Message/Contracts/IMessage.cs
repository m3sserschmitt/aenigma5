namespace Message.Contracts;

public interface IMessage
{
    byte[]? Content { get; set; }
}
