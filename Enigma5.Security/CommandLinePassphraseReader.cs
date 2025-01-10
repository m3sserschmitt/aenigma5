/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.Security.Contracts;

namespace Enigma5.Security;

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
