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
using Enigma5.Crypto.Extensions;

namespace Enigma5.App.Models;

public class SignatureRequest: IValidatable
{
    public string? Nonce { get; set; }

    public SignatureRequest(string nonce)
    {
        Nonce = nonce;
    }

    public SignatureRequest() { }

    public IEnumerable<Error> Validate()
    {
        if(string.IsNullOrWhiteSpace(Nonce))
        {
            yield return new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(Nonce)]);
        }

        if(!Nonce.IsValidBase64())
        {
            yield return new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(Nonce)]);
        }
    }
}
