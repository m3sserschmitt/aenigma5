/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

namespace Enigma5.Crypto;

public static class KernelKey
{
    public static int Create(string keyName, string keyMaterial, string description, KernelKeyring ringId)
    => Native.CreateKey(keyName, keyMaterial, (uint)keyMaterial.Length, description, ringId);

    public static int SearchKey(string keyName, string description, KernelKeyring ringId)
    => Native.SearchKey(keyName, description, ringId);

    public static string? ReadKey(int keyId)
    {
        try
        {
            var buffer = new byte[Constants.KernelKeyMaxSize];
            if (Native.ReadKey(keyId, buffer) < 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(buffer);
        }
        catch
        {
            return null;
        }
    }

    public static int RemoveKey(int keyId)
    => Native.RemoveKey(keyId);
}
