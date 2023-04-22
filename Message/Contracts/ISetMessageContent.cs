namespace Message.Contracts;

public interface ISetMessageContent
{
    IEncryptMessage SetMessageContent(byte[] content);
}
