/*
    Aenigma - Federated messaging system
    Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Enigma5.App.Models;

public class SharedDataDto
{
    [Description("Shared data object identifier in GUID format.")]
    public string? Tag { get; set; }

    [Description("Url of the created object.")]
    public string? ResourceUrl { get; set; }

    [Description("Shared object signed data.")]
    public string? Data { get; set; }

    [JsonIgnore]
    public FileStream? File { get; set; }

    [Description("Public key in PEM format used during object creation.")]
    public string? PublicKey { get; set; }

    [Description("Object expiration date.")]
    public DateTimeOffset? ValidUntil { get; set; }
}
