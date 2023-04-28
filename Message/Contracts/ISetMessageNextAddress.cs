namespace Message.Contracts;

public interface ISetMessageNextAddress
{
    IEncryptMessage SetNextAddress(byte[] address);
}
