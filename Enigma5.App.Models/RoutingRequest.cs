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

using System.Buffers.Text;
using Enigma5.App.Models.Contracts;

namespace Enigma5.App.Models;

public class RoutingRequest: IValidatable
{
    public string? Payload { get; set; }

    public RoutingRequest(string payload)
    {
        Payload = payload;
    }

    public RoutingRequest() { }

    public IEnumerable<Error> Validate()
    {
        if(string.IsNullOrWhiteSpace(Payload))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Payload)]);
        }

        if(Payload is not null && !Base64.IsValid(Payload))
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Payload)]);
        }
    }
}
