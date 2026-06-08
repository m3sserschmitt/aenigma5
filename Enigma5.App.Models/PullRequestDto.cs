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

using System.Text.Json.Serialization;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

[method: JsonConstructor]
public class PullRequestDto(long? infId = null) : IValidatable
{
    public long? InfId { get; private set; } = infId;

    public HashSet<ErrorDto> Validate()
    {
        var errors = new HashSet<ErrorDto>();
        if (InfId < 0)
        {
            errors.AddError(ValidationErrorsDto.INVALID_VALUE_FOR_PROPERTY, nameof(InfId));
        }
        return errors;
    }
}
