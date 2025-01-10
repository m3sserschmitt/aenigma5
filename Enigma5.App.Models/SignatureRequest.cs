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
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;
using Enigma5.Crypto.Extensions;

namespace Enigma5.App.Models;

[method: JsonConstructor]
public class SignatureRequest(string? nonce = null) : IValidatable
{
    public string? Nonce { get; set; } = nonce;

    public HashSet<Error> Validate()
    {
        var errors = new HashSet<Error>();
        if(string.IsNullOrWhiteSpace(Nonce))
        {
            errors.AddError(ValidationErrors.NULL_REQUIRED_PROPERTIES, nameof(Nonce));
        }
        else if(!Nonce.IsValidBase64())
        {
            errors.AddError(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(Nonce));
        }
        return errors;
    }
}
