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

namespace Enigma5.App.Models;

public class PeerDto
{
    public long? Id { get; set; }

    public string? Host { get; set; }

    public string? Address { get; set; }

    public bool Connected { get; set; }

    public bool Equals(PeerDto? peer) => peer == this;

    public static bool operator ==(PeerDto? obj1, PeerDto? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        return obj1.Address == obj2.Address;
    }

    public static bool operator !=(PeerDto? obj1, PeerDto? obj2) => !(obj1 == obj2);

    public override bool Equals(object? obj) => Equals(obj as PeerDto);

    public override int GetHashCode() => Address?.GetHashCode() ?? 0;
}
