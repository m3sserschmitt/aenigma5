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

using System.Text.Json.Serialization;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;
using Enigma5.Crypto.Extensions;

namespace Enigma5.App.Models;

[method: JsonConstructor]
public class SharedDataCreate(string? publicKey = null, string? signedData = null, int accessCount = 1): IValidatable
{
    public string? PublicKey { get; private set; } = publicKey;

    public string? SignedData { get; private set; } = signedData;

    public int AccessCount { get; private set; } = accessCount;

    public HashSet<Error> Validate()
    {
        var errors = new HashSet<Error>();

        if(string.IsNullOrWhiteSpace(PublicKey))
        {
            errors.AddError(ValidationErrors.NULL_REQUIRED_PROPERTIES, nameof(PublicKey));
        }
        else if(!PublicKey.IsValidPublicKey())
        {
            errors.AddError(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(PublicKey));
        }

        if(string.IsNullOrWhiteSpace(SignedData))
        {
            errors.AddError(ValidationErrors.NULL_REQUIRED_PROPERTIES, nameof(SignedData));
        }
        else if(!SignedData.IsValidBase64())
        {
            errors.AddError(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(SignedData));
        }

        if(AccessCount < 0)
        {
            errors.AddError(ValidationErrors.INVALID_VALUE_FOR_PROPERTY, nameof(AccessCount));
        }

        return errors;
    }
}
