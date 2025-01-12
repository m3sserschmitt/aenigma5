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

using Enigma5.Crypto.Extensions;

namespace Enigma5.Crypto;

public static class CertificateHelper
{
    public static byte[]? GetAddressFromPublicKey(string? publicKey)
    {
        try
        {
            var content = publicKey.GetPublicKeyBase64();
            
            if(content is null)
            {
                return null;
            }

            return HashProvider.Sha256(Convert.FromBase64String(content));
        }
        catch
        {
            return null;
        }
    }

    public static string GetHexAddressFromPublicKey(string? publicKey)
    {
        var hash = GetAddressFromPublicKey(publicKey);

        if(hash is null)
        {
            return string.Empty;
        }
        
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }
}
