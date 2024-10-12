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

using System.Text.Json.Serialization;

namespace Enigma5.App.Data;

[method: JsonConstructor]
public class Neighborhood(HashSet<string> neighbors, string address, string? hostname)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hostname { get; private set; } = hostname;

    public string Address { get; private set; } = address;

    public HashSet<string> Neighbors { get; private set; } = new HashSet<string>(neighbors);

    public static bool operator ==(Neighborhood? obj1, Neighborhood? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        return obj1.Neighbors.SetEquals(obj2.Neighbors)
        && obj1.Hostname == obj2.Hostname
        && obj1.Address == obj2.Address;
    }

    public static bool operator !=(Neighborhood? obj1, Neighborhood? obj2) => !(obj1 == obj2);

    public override bool Equals(object? obj) => obj is Neighborhood other && Address == other.Address;

    public override int GetHashCode() => Address.GetHashCode();
}
