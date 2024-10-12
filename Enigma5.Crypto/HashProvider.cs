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

using System.Security.Cryptography;
using System.Text;

namespace Enigma5.Crypto;

public static class HashProvider
{
    public static string ToHex(byte[] data)
    => BitConverter.ToString(data).Replace("-", string.Empty).ToLower();

    public static byte[] Sha256(byte[] data) => SHA256.HashData(data);

    public static byte[] FromHexString(string hexString)
    {
        if (hexString.Length % 2 != 0)
            throw new ArgumentException("Invalid hexadecimal string.");

        int byteCount = hexString.Length / 2;
        byte[] byteArray = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            string byteString = hexString.Substring(i * 2, 2);
            byteArray[i] = Convert.ToByte(byteString, 16);
        }

        return byteArray;
    }

    public static string Sha256Hex(byte[] data)
    => ToHex(Sha256(data));

    public static string Sha256Hex(string data)
    => Sha256Hex(Encoding.UTF8.GetBytes(data));
}
