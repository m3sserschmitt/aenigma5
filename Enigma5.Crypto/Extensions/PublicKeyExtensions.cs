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

using System.Text.RegularExpressions;

namespace Enigma5.Crypto.Extensions;

public static partial class PublicKeyExtensions
{
    private static bool IsValidKey(this string? key, Func<Regex> regex)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(key))
            {
                return false;
            }
            
            var matches = regex.Invoke().Match(key);
            
            if (matches.Success)
            {
                var base64Content = matches.Groups[1].Value
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "");
                return base64Content.IsValidBase64();
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPublicKey(this string? publicKey) => publicKey.IsValidKey(PublicKeyRegex);

    public static bool IsValidPrivateKey(this string? privateKey) => privateKey.IsValidKey(PrivateKeyRegex);

    [GeneratedRegex(@"^-----BEGIN(?: [A-Z]+)* PRIVATE KEY-----\s*([A-Za-z0-9+/=\r\n]+?)\s*-----END(?: [A-Z]+)* PRIVATE KEY-----$", RegexOptions.Multiline)]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"^-----BEGIN(?: [A-Z]+)* PUBLIC KEY-----\s*([A-Za-z0-9+/=\r\n]+?)\s*-----END(?: [A-Z]+)* PUBLIC KEY-----$", RegexOptions.Multiline)]
    private static partial Regex PublicKeyRegex();
}
