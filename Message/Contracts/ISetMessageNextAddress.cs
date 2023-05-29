namespace Enigma5.Message.Contracts;

public interface ISetMessageNextAddress
{
    IEncryptMessage SetNextAddress(byte[] address);
}
