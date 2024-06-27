namespace Enigma5.Structures.Contracts;

public interface IEncryptMessage
{
    IOnionBuilder Seal(string key);

    IOnionBuilder SealEx(string keyPath);
}
