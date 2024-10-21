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

using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

public partial class AdjacencyList : IValidatable
{
    public string? Address { get; set; }

    public string? Hostname { get; set; }

    public HashSet<string>? Neighbors { get; set; }

    public IEnumerable<Error> Validate()
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Address)]);
        }

        if (Neighbors is null)
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Neighbors)]);
        }

        if (Address is not null && !Address.IsValidAddress())
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Address)]);
        }

        if (Neighbors is not null && Neighbors.Any(item => !item.IsValidAddress()))
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Neighbors)]);
        }
    }
}
