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

namespace Enigma5.App.Common.Extensions;

public static partial class StringExtensions
{
    private static readonly Regex OnionRegex = OnionAddressRegex();

    public static bool IsValidOnionAddress(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return OnionRegex.IsMatch(value);
    }

    public static string ToQrCode(this string text, int pixelsPerModule = 20)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(data);
        byte[] pngBytes = qrCode.GetGraphic(pixelsPerModule);

        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }

    [GeneratedRegex(@"^(?:[a-z2-7]{16}|[a-z2-7]{56})\.onion$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex OnionAddressRegex();
}
