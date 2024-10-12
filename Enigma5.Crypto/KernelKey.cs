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

using System.Runtime.InteropServices;

namespace Enigma5.Crypto;

public static class KernelKey
{
    public static int Create(string keyName, byte[] keyMaterial, string description, KernelKeyring ringId)
    {
        var nativeBuffer = KeyUtil.CopyKeyToNativeBuffer(keyMaterial);

        if (nativeBuffer == IntPtr.Zero)
        {
            return -1;
        }

        var keyId = Native.CreateKey([.. keyName], nativeBuffer, (uint)keyMaterial.Length, [.. description], ringId);

        KeyUtil.FreeKeyNativeBuffer(nativeBuffer, keyMaterial);

        return keyId;
    }

    public static int SearchKey(string keyName, string description, KernelKeyring ringId)
    => Native.SearchKey([.. keyName], [.. description], ringId);

    public static byte[]? ReadKey(int keyId)
    {
        IntPtr nativeBuffer = Marshal.AllocHGlobal(Constants.KernelKeyMaxSize);
        int keySize = Native.ReadKey(keyId, nativeBuffer);

        if (keySize < 0 || keySize > Constants.KernelKeyMaxSize)
        {
            KeyUtil.FreeKeyNativeBuffer(nativeBuffer, Constants.KernelKeyMaxSize);
            return null;
        }

        var key = KeyUtil.CopyKeyFromNativeBuffer(nativeBuffer, keySize);
        KeyUtil.FreeKeyNativeBuffer(nativeBuffer, Constants.KernelKeyMaxSize);

        return key;
    }

    public static int RemoveKey(int keyId)
    => Native.RemoveKey(keyId);
}
