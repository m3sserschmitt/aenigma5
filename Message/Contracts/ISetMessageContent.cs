namespace Message.Contracts;

public interface ISetMessageContent
{
    ISetMessageNextAddress SetMessageContent(byte[] content);
}
