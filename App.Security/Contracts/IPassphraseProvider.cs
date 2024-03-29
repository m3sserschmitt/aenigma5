namespace Enigma5.App.Security.Contracts;

public interface IPassphraseProvider
{
    char[] ProvidePassphrase();
}
