using Enigma5.App.Security.Contracts;

namespace Enigma5.App.Security;

public class CommandLinePassphraseReader : IPassphraseProvider
{
#if DEBUG
#else
    private static readonly int PASSPHRASE_MAX_LENGTH = 128;
#endif

    public char[] ProvidePassphrase() => ReadPassphrase();

    private static char[] ReadPassphrase()
    {
#if DEBUG
        return ['1', '2', '3', '4'];
#else
        var i = 0;
        var password = new char[PASSPHRASE_MAX_LENGTH];
        ConsoleKeyInfo key;

        Console.Write("Private key passkey (will not be echoed): ");

        do
        {
            key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Backspace)
            {
                if (i == 0)
                {
                    continue;
                }

                i -= 1;

                continue;
            }

            if (key.Key != ConsoleKey.Enter)
            {
                password[i] = key.KeyChar;
                i += 1;
            }
        } while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();

        var validChars = new char[i];

        for (int j = 0; j < i; j++)
        {
            validChars[j] = password[j];
        }

        Array.Clear(password);

        return validChars;
#endif
    }
}
