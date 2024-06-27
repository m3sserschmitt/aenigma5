namespace Enigma5.Structures.Contracts;

public interface ISetMessageNextAddress
{
    IEncryptMessage SetNextAddress(byte[] address);
}
