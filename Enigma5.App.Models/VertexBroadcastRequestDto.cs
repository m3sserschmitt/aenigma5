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

using System.Text.Json;
using System.Text.Json.Serialization;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;
using Enigma5.Crypto.Extensions;

namespace Enigma5.App.Models;

public class VertexBroadcastRequestDto : IValidatable
{
    private string? _signedData;

    [JsonIgnore]
    public NeighborhoodDto Neighborhood { get; private set; } = new();

    public string? PublicKey { get; private set; }

    public string? SignedData
    {
        get => _signedData;
        private set
        {
            try
            {
                _ = value ?? throw new Exception();
                _ = PublicKey ?? throw new Exception();

                var decodedData = Convert.FromBase64String(value);
                var adjacencyList = decodedData.GetStringDataFromSignature(PublicKey) ?? throw new Exception();

                Neighborhood = JsonSerializer.Deserialize<NeighborhoodDto>(adjacencyList) ?? throw new Exception();
                _signedData = value;
            }
            catch (Exception)
            {
                Neighborhood = new();
                _signedData = null;
            }
        }
    }

    [method: JsonConstructor]
    public VertexBroadcastRequestDto(string? publicKey = null, string? signedData = null)
    {
        PublicKey = publicKey;
        SignedData = signedData;
    }

    public HashSet<ErrorDto> Validate()
    {
        var validationResults = new HashSet<ErrorDto>();

        if (string.IsNullOrWhiteSpace(PublicKey))
        {
            validationResults.AddError(ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(PublicKey));
        }
        else if (!PublicKey.IsValidPublicKey())
        {
            validationResults.AddError(ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(PublicKey));
        }

        if (string.IsNullOrEmpty(_signedData))
        {
            validationResults.AddError(ValidationErrorsDto.PROPERTIES_FORMAT_COULD_NOT_BE_VERIFIED, nameof(SignedData));
        }
        else
        {
            validationResults.AddErrors(Neighborhood.Validate());
        }

        return validationResults;
    }
}
