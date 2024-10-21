/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.Crypto.DataProviders;
using FluentAssertions;
using Xunit;

namespace Enigma5.Structures.Tests;

public class SealUnsealOnionTests
{
    [Fact]
    public void ShouldSealAndUnseal()
    {
        // Arrange
        var keys = new string[] { PKey.PublicKey2, PKey.PublicKey1 };
        var addresses = new string[] { PKey.Address2, PKey.Address1 };
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        using var parser1 = OnionParser.Factory.Create(Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Passphrase);
        using var parser2 = OnionParser.Factory.Create(Encoding.UTF8.GetBytes(PKey.PrivateKey2), PKey.Passphrase);

        // Act
        var onion = OnionBuilder.CreateOnion(plaintext, keys, addresses);
        var firstParse = parser1.Parse(new Onion { Content = onion! });
        var secondParse = parser2.Parse(new Onion { Content = parser1.Content! });

        // Assert
        firstParse.Should().BeTrue();
        secondParse.Should().BeTrue();
        parser1.NextAddress.Should().Be(PKey.Address1);
        parser2.NextAddress.Should().Be(PKey.Address2);
        parser2.Content.Should().HaveCount(8);
        Assert.Equal(plaintext, parser2.Content);
    }
}
