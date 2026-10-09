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

using System.Runtime.InteropServices;

namespace Enigma5.Crypto.Tests;

public class KeyUtilTests
{
    [Fact]
    public void CopyKeyFromNativeBuffer_copies_the_given_number_of_bytes()
    {
        byte[] source = [1, 2, 3, 4, 5];
        var buffer = Marshal.AllocHGlobal(source.Length);
        try
        {
            Marshal.Copy(source, 0, buffer, source.Length);

            Assert.Equal(source, KeyUtil.CopyKeyFromNativeBuffer(buffer, source.Length));
            Assert.Equal([1, 2], KeyUtil.CopyKeyFromNativeBuffer(buffer, 2));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [Fact]
    public void CopyKeyFromNativeBuffer_returns_null_for_a_null_pointer()
    {
        Assert.Null(KeyUtil.CopyKeyFromNativeBuffer(IntPtr.Zero, 4));
    }
}
