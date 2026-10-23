/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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
    public Task<char[]?> ProvidePassphraseAsync() => Task.Run<char[]?>(() =>
    {
        Console.Write("Private key passkey (will not be echoed): ");
        var passphrase = Read(() => Console.ReadKey(true), Crypto.Constants.KernelKeyMaxSize);
        Console.WriteLine();
        return passphrase;
    });

    // Reads keys up to Enter. Like the passphrase field of the dashboard, it takes at most maxLength characters
    // and ignores further ones; Backspace still works. Keys that give no character (arrows, function keys) are ignored.
    public static char[] Read(Func<ConsoleKeyInfo> readKey, int maxLength)
    {
        var buffer = new char[maxLength];
        var length = 0;
        try
        {
            for (var key = readKey(); key.Key != ConsoleKey.Enter; key = readKey())
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (length > 0)
                    {
                        length -= 1;
                    }
                }
                else if (key.KeyChar != '\0' && length < maxLength)
                {
                    buffer[length] = key.KeyChar;
                    length += 1;
                }
            }
            return buffer[..length];
        }
        finally
        {
            Array.Clear(buffer);
        }
    }
}
