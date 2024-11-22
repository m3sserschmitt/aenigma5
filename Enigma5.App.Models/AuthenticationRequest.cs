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
using Enigma5.Crypto.Extensions;

namespace Enigma5.App.Models;

public class AuthenticationRequest: IValidatable
{
    public string? PublicKey { get; set; }

    public string? Signature { get; set; }

    public HashSet<Error> Validate()
    {
        var errors = new HashSet<Error>();
        if(string.IsNullOrWhiteSpace(PublicKey))
        {
            errors.AddError(ValidationErrors.NULL_REQUIRED_PROPERTIES, nameof(PublicKey));
        }

        if(string.IsNullOrWhiteSpace(Signature))
        {
            errors.AddError(ValidationErrors.NULL_REQUIRED_PROPERTIES, nameof(Signature));
        }

        if(!string.IsNullOrWhiteSpace(PublicKey) && !PublicKey.IsValidPublicKey())
        {
            errors.AddError(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(PublicKey));
        }

        if(!string.IsNullOrWhiteSpace(Signature) && !Signature.IsValidBase64())
        {
            errors.AddError(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(Signature));
        }

        return errors;
    }
}
