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

using System.Text.Json;
using System.Text.Json.Serialization;
using Enigma5.App.Models.Contracts;
using Enigma5.Crypto.Extensions;

namespace Enigma5.App.Models;

public class VertexBroadcastRequest : IValidatable
{
    private string? _signedData;

    [JsonIgnore]
    public AdjacencyList AdjacencyList { get; private set; } = new();

    public string? PublicKey { get; set; }

    public string? SignedData
    {
        get => _signedData;
        set
        {
            try
            {
                _ = value ?? throw new Exception();
                _ = PublicKey ?? throw new Exception();

                var decodedData = Convert.FromBase64String(value);
                var adjacencyList = decodedData.GetStringDataFromSignature(PublicKey) ?? throw new Exception();

                AdjacencyList = JsonSerializer.Deserialize<AdjacencyList>(adjacencyList) ?? throw new Exception();
                _signedData = value;
            }
            catch (Exception)
            {
                AdjacencyList = new();
                _signedData = null;
            }
        }
    }

    public VertexBroadcastRequest(string publicKey, string signedData)
    {
        PublicKey = publicKey;
        SignedData = signedData;
    }

    public VertexBroadcastRequest() { }

    public IEnumerable<Error> Validate()
    {
        var validationResults = new List<Error>();

        if (string.IsNullOrWhiteSpace(PublicKey))
        {
            validationResults.Add(new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(PublicKey)]));
        }

        if (PublicKey is not null && !PublicKey.IsValidPublicKey())
        {
            validationResults.Add(new Error(ValidationErrors.NULL_REQUIRED_PROPERTIES, [nameof(PublicKey)]));
        }

        if (_signedData is null)
        {
            validationResults.Add(new Error(ValidationErrors.PROPERTIES_NOT_IN_CORRECT_FORMAT, [nameof(SignedData)]));
        }
        else
        {
            validationResults.AddRange(AdjacencyList.Validate());
        }

        return validationResults;
    }
}
