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

using System.Text.Json.Serialization;

namespace Enigma5.App.Models;

[method: JsonConstructor]
public class Error(string? message = null, HashSet<string>? properties = null)
{
    public string? Message { get; private set; } = message;

    public HashSet<string>? Properties { get; private set; } = properties;

    public bool Equals(Error? error)
    {
        if(error is null)
        {
            return false;
        }

        return error.Message == Message;
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(this, obj))
        {
            return true;
        }

        return Equals(obj as Error);
    }

    public override int GetHashCode() => Message is null ? 0 : Message.GetHashCode();

    public static bool operator==(Error e1, Error e2) => e1.Message == e2.Message;

    public static bool operator!=(Error e1, Error e2) => !(e1 == e2);
}
