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

using System.Text;

namespace Enigma5.Crypto.Tests;

public class HashProviderTests
{
    // The SHA-256 value of "abc" from the standard's examples.
    private const string Sha256OfAbc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public void Sha256Hex_gives_the_known_value_for_a_text()
    {
        Assert.Equal(Sha256OfAbc, HashProvider.Sha256Hex("abc"));
        Assert.Equal(Sha256OfAbc, HashProvider.Sha256Hex(Encoding.UTF8.GetBytes("abc")));
    }

    [Fact]
    public void Sha256_gives_32_bytes()
    {
        Assert.Equal(32, HashProvider.Sha256([1, 2, 3]).Length);
    }

    [Fact]
    public void ToHex_writes_two_lower_case_digits_for_each_byte()
    {
        Assert.Equal("000fa0ff", HashProvider.ToHex([0x00, 0x0F, 0xA0, 0xFF]));
        Assert.Equal(string.Empty, HashProvider.ToHex([]));
    }
}
