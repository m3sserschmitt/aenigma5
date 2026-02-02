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

using System.Text.Json;
using System.Text.Json.Serialization;
using Org.Webpki.JsonCanonicalizer;

namespace Enigma5.App.Common.Extensions;

public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T CopyBySerialization<T>(this T source) where T : class
    {
        var serializedData = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<T>(serializedData)!;
    }

    public static string CanonicallySerialize(this object source)
    {
        var serializedData = JsonSerializer.Serialize(source, jsonSerializerOptions);
        var jsonCanonicalizer = new JsonCanonicalizer(serializedData);
        return jsonCanonicalizer.GetEncodedString();
    }

    public static string ToQrCode(this object source, int pixelsPerModule = 20)
    => source.CanonicallySerialize().ToQrCode(pixelsPerModule);
}
