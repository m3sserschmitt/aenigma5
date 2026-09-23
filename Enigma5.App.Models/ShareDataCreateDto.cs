/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

namespace Enigma5.App.Models;

[method: JsonConstructor]
public class SharedDataCreateDto([Required]string? publicKey = null, string? signedData = null, int? accessCount = 1): IValidatable
{

    [Description("Public key of the user creating the shared data in PEM format.")]
    public string? PublicKey { get; private set; } = publicKey;


    [Description("Shared data with attached signature in base64 format.")]
    public string? SignedData { get; private set; } = signedData;


    [Description("Maximum data access count controlling how many times the data can be retrieved.")]
    public int? AccessCount { get; private set; } = accessCount;

    public HashSet<ErrorDto> Validate()
    {
        var errors = new HashSet<ErrorDto>();

        if(string.IsNullOrWhiteSpace(PublicKey))
        {
            errors.AddError(ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(PublicKey));
        }
        else if(!PublicKey.IsValidPublicKey())
        {
            errors.AddError(ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(PublicKey));
        }

        if(string.IsNullOrWhiteSpace(SignedData))
        {
            errors.AddError(ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(SignedData));
        }
        else if(!SignedData.IsValidBase64())
        {
            errors.AddError(ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(SignedData));
        }

        if(AccessCount <= 0)
        {
            errors.AddError(ValidationErrorsDto.INVALID_VALUE_FOR_PROPERTY, nameof(AccessCount));
        }

        return errors;
    }
}
