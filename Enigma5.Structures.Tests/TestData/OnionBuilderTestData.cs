/*
    Aenigma - Onion Routing based messaging application
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

namespace Enigma5.Structures.Tests.TestData;

public class OnionBuilderTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { GenerateBytes(128), GenerateBytes(32), new byte[] { 1, 188 }, 446 };
        yield return new object[] { GenerateBytes(256), GenerateBytes(32), new byte[] { 2, 60 }, 574 };
        yield return new object[] { GenerateBytes(512), GenerateBytes(32), new byte[] { 3, 60 }, 830 };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static byte[] GenerateBytes(int count)
    {
        var bytes = new byte[count];
        new Random().NextBytes(bytes);
        return bytes;
    }
}
