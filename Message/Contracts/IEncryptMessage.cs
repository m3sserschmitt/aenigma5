namespace Message.Contracts;

public interface IEncryptMessage
{
    IOnionBuilder Seal(string key);

    IOnionBuilder SealEx(string keyPath);
}
