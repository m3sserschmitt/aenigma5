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

namespace Enigma5.App.Models;

public class ServerInfoDto
{
    [Description("Server public key in PEM format.")]
    public string? PublicKey { get; set; }

    [Description("Sha256 derived from public key.")]
    public string? Address { get; set; }

    [Description("Sha256 derived from local ledger.")]
    public string? GraphVersion { get; set; }

    [Description("Onion service base API address.")]
    public string? OnionService { get; set; }

    [Description("Base API address.")]
    public string? Hostname { get; set; }
}
