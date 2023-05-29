namespace Enigma5.Message.Contracts;

public interface ISetMessageContent
{
    ISetMessageNextAddress SetMessageContent(byte[] content);
}
