using System.Text.Json;
using System.Text.Json.Serialization;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.Extensions;

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

                var decodedData = Convert.FromBase64String(value);
                var adjacencyList = decodedData.GetStringDataFromSignature() ?? throw new Exception();

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
