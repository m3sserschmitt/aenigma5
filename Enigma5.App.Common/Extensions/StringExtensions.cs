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

using System.Text.RegularExpressions;
using QRCoder;
using System.Buffers.Text;
using System.Net;

namespace Enigma5.App.Common.Extensions;

public static partial class StringExtensions
{
    public static bool MatchUrl(this string? url, IPAddress ipAddress, int port)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl))
        {
            return false;
        }

        if (!IPAddress.TryParse(parsedUrl.Host, out var parsedIpAddress))
        {
            return false;
        }

        if (parsedIpAddress.Equals(IPAddress.Any))
        {
            return parsedUrl.Port == port;
        }

        return parsedIpAddress.Normalize().Equals(ipAddress.Normalize()) && parsedUrl.Port == port;
    }

    private static readonly Regex OnionRegex = OnionAddressRegex();

    public static bool IsValidOnionAddress(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return OnionRegex.IsMatch(value);
    }

    public static bool IsValidOnionUrl(this string? value)
    => !string.IsNullOrWhiteSpace(value)
    && Uri.TryCreate(value, UriKind.Absolute, out var sourceUri)
    && sourceUri.Host.IsValidOnionAddress();

    public static string ToQrCode(this string text, int pixelsPerModule = 20)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(data);
        byte[] pngBytes = qrCode.GetGraphic(pixelsPerModule);

        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }

    public static bool IsValidAddress(this string? address)
    => !string.IsNullOrWhiteSpace(address) && AddressRegex().IsMatch(address);

        private static bool IsValidKey(this string? key, Func<Regex> regex)
    => key.GetKeyBase64Content(regex).IsValidBase64();

    private static string? GetKeyBase64Content(this string? key, Func<Regex> regex)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var matches = regex.Invoke().Match(key);

            if (matches.Success)
            {
                return matches.Groups[1].Value.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(" ", string.Empty);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool IsValidPublicKey(this string? publicKey) => publicKey.IsValidKey(PublicKeyRegex);

    public static bool IsValidPrivateKey(this string? privateKey) => privateKey.IsValidKey(PrivateKeyRegex);

    public static string? GetPublicKeyBase64(this string? publicKey) => publicKey.GetKeyBase64Content(PublicKeyRegex);

    [GeneratedRegex(@"^-----BEGIN(?: [A-Z]+)* PRIVATE KEY-----\s*([A-Za-z0-9+/=\r\n]+?)\s*-----END(?: [A-Z]+)* PRIVATE KEY-----$", RegexOptions.Multiline)]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"^-----BEGIN(?: [A-Z]+)* PUBLIC KEY-----\s*([A-Za-z0-9+/=\r\n]+?)\s*-----END(?: [A-Z]+)* PUBLIC KEY-----$", RegexOptions.Multiline)]
    private static partial Regex PublicKeyRegex();

    public static bool IsValidBase64(this string? data) => !string.IsNullOrWhiteSpace(data) && Base64.IsValid(data);

    [GeneratedRegex(@"^[a-f0-9]{64}$")]
    private static partial Regex AddressRegex();

    [GeneratedRegex(@"^(?:[a-z2-7]{16}|[a-z2-7]{56})\.onion$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex OnionAddressRegex();
}
