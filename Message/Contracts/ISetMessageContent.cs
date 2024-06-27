namespace Enigma5.Structures.Contracts;

public interface ISetMessageContent
{
    ISetMessageNextAddress SetMessageContent(byte[] content);
}
