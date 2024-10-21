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

namespace Enigma5.Crypto;

public static class CertificateHelper
{
    public static byte[] GetAddressFromPublicKey(string publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            return [];
        }

        try
        {
            string[] lines = publicKey.Split('\n').Where(l => l.Length != 0).ToArray();

            StringBuilder base64ContentBuilder = new();
            for (int i = 1; i < lines.Length - 1; i++)
            {
                base64ContentBuilder.Append(lines[i].Trim());
            }

            return HashProvider.Sha256(Convert.FromBase64String(base64ContentBuilder.ToString()));
        }
        catch
        {
            return [];
        }
    }

    public static string GetHexAddressFromPublicKey(string? publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            return string.Empty;
        }

        var hash = GetAddressFromPublicKey(publicKey);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }
}
