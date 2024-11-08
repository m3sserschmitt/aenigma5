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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.Crypto.Tests.TestData;

[ExcludeFromCodeCoverage]
public class SealerData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x23, 0x56, 0x11 }, PKey.PublicKey1, 256 + 12 + 16 + 12 };
        yield return new object[] { new byte[] { 0x05, 0x06, 0x07, 0x08, 0x03, 0x02 }, PKey.PublicKey2, 256 + 12 + 16 + 6 };
        yield return new object[] { new byte[] { 0x03, 0x04, 0x07, 0x01, 0x03, 0x02, 0x09, 0x07 }, PKey.PublicKey3, 256 + 12 + 16 + 8 };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
