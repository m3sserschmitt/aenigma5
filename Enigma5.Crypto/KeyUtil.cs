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

using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public static class KeyUtil
{
    public static bool FreeKeyNativeBuffer(IntPtr nativeBuffer, int bytesCount)
    {
        try
        {
            Marshal.Copy(new byte[bytesCount], 0, nativeBuffer, bytesCount);
            Marshal.FreeHGlobal(nativeBuffer);
            return true;
        }
        catch(Exception)
        {
            return false;
        }
    }

    public static bool FreeKeyNativeBuffer(IntPtr nativeBuffer, byte[] keyMaterial)
    {
        try
        {
            FreeKeyNativeBuffer(nativeBuffer, keyMaterial.Length);
            Array.Clear(keyMaterial);
            return true;
        }
        catch(Exception)
        {
            return false;
        }
    }

    public static byte[]? CopyKeyFromNativeBuffer(nint source, int bytesCount)
    {
        try
        {
            var managedBytes = new byte[bytesCount];
            Marshal.Copy(source, managedBytes, 0, bytesCount);

            return managedBytes;
        }
        catch(Exception)
        {
            return null;
        }
    }

    private static readonly byte[] nullByte = [ 0 ];

    public static nint CopyKeyToNativeBuffer(byte[] source)
    {
        try
        {
            var nativeBuffer = Marshal.AllocHGlobal(source.Length + 1);
            
            Marshal.Copy(source, 0, nativeBuffer, source.Length);
            Marshal.Copy(nullByte, 0, nativeBuffer + source.Length, nullByte.Length);
            
            return nativeBuffer;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }
}
