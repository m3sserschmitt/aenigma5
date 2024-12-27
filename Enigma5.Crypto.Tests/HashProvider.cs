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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Enigma5.Crypto.Tests;

[ExcludeFromCodeCoverage]
public class HashProviderTests
{
    [Theory]
    [InlineData("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    [InlineData("test 123", "f7ef53d21502321eaecb78bb405b7ff266253b4a27d89b9b8c4da5847cdd1b9d")]
    [InlineData("test 123 123", "97155afa49dc5a5c468c259306d22a95eddd9ae257d9ccf3356f364c447e4907")]
    public void ShouldProduceCorrectHash(string input, string expected)
    {
        // Arrange

        // Act
        var actual = HashProvider.Sha256Hex(Encoding.UTF8.GetBytes(input));

        // Assert
        actual.Should().Be(expected);
    }
}
