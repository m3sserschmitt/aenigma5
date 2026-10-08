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
using Enigma5.Crypto.Extensions;
using Enigma5.Tests.Base;

namespace Enigma5.Crypto.Tests.Extensions;

public class ByteArrayExtensionsTests
{
    // Signed data is the data followed by a signature as long as the key: 256 bytes for the test keys.
    private static byte[] Signed(string text) => [.. Encoding.UTF8.GetBytes(text), .. new byte[256]];

    [Fact]
    public void GetDataFromSignature_returns_the_part_before_the_signature()
    {
        Assert.Equal(Encoding.UTF8.GetBytes("hello"), Signed("hello").GetDataFromSignature(TestKeys.PublicKey1));
        Assert.Equal("hello", Signed("hello").GetStringDataFromSignature(TestKeys.PublicKey1));
    }

    [Fact]
    public void GetDataFromSignature_returns_null_for_missing_input()
    {
        Assert.Null(((byte[]?)null).GetDataFromSignature(TestKeys.PublicKey1));
        Assert.Null(((byte[]?)null).GetStringDataFromSignature(TestKeys.PublicKey1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(255)]
    [InlineData(256)]
    public void GetDataFromSignature_returns_null_for_input_without_room_for_data(int length)
    {
        Assert.Null(new byte[length].GetDataFromSignature(TestKeys.PublicKey1));
    }

    [Fact]
    public void GetDataFromSignature_returns_one_byte_for_the_shortest_signed_data()
    {
        Assert.Equal([0x00], new byte[257].GetDataFromSignature(TestKeys.PublicKey1));
    }
}
