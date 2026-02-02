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
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

[method: JsonConstructor]
public class NeighborhoodDto(
    string? address = null,
    string? hostname = null,
    string? onionService = null,
    HashSet<string>? neighbors = null,
    DateTimeOffset? lastUpdate = null) : IValidatable
{
    public string? Address { get; private set; } = address;

    public string? Hostname { get; private set; } = hostname;

    public string? OnionService { get; private set; } = onionService;

    public HashSet<string>? Neighbors { get; private set; } = neighbors;

    public DateTimeOffset? LastUpdate { get; private set; } = lastUpdate;

    public HashSet<ErrorDto> Validate()
    {
        var errors = new HashSet<ErrorDto>();

        if (string.IsNullOrWhiteSpace(Address))
        {
            errors.AddError(ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(Address));
        }
        else if (!Address.IsValidAddress())
        {
            errors.AddError(ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(Address));
        }

        if (Neighbors is null)
        {
            errors.AddError(ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(Neighbors));
        }
        else if (Neighbors.Any(item => !item.IsValidAddress()))
        {
            errors.AddError(ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(Neighbors));
        }

        return errors;
    }
}
