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

namespace Enigma5.Security.Tests;

public class CommandLinePassphraseReaderTests
{
    private static readonly ConsoleKeyInfo Enter = new('\r', ConsoleKey.Enter, false, false, false);

    private static readonly ConsoleKeyInfo Backspace = new('\b', ConsoleKey.Backspace, false, false, false);

    private static readonly ConsoleKeyInfo LeftArrow = new('\0', ConsoleKey.LeftArrow, false, false, false);

    private static IEnumerable<ConsoleKeyInfo> Typed(string text) => text.Select(c => new ConsoleKeyInfo(c, ConsoleKey.A, false, false, false));

    private static string Read(int maxLength, params IEnumerable<ConsoleKeyInfo>[] keys)
    {
        using var queue = keys.SelectMany(key => key).GetEnumerator();
        return new string(CommandLinePassphraseReader.Read(() => queue.MoveNext() ? queue.Current : throw new InvalidOperationException("Read past Enter."), maxLength));
    }

    [Fact]
    public void The_characters_up_to_Enter_are_the_passphrase()
    {
        Assert.Equal("pass phrase", Read(256, Typed("pass phrase"), [Enter]));
    }

    [Fact]
    public void Characters_beyond_the_limit_are_ignored_without_an_error()
    {
        Assert.Equal(new string('x', 256), Read(256, Typed(new string('x', 300)), [Enter]));
    }

    [Fact]
    public void Backspace_removes_the_last_character_also_at_the_limit()
    {
        Assert.Equal("abd", Read(3, Typed("abcz"), [Backspace], Typed("d"), [Enter]));
        Assert.Equal("a", Read(256, [Backspace], Typed("ab"), [Backspace], [Enter]));
    }

    [Fact]
    public void Keys_that_give_no_character_are_ignored()
    {
        Assert.Equal("ab", Read(256, Typed("a"), [LeftArrow], Typed("b"), [Enter]));
    }

    [Fact]
    public void The_limit_of_the_dashboard_and_the_keyring_is_256_characters()
    {
        Assert.Equal(256, Crypto.Constants.KernelKeyMaxSize);
    }
}
