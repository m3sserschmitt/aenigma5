using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Enigma5.Crypto;

namespace Enigma5.App.Models;

public class VertexBroadcast
{
    private string _signedData;

    [JsonIgnore]
    public AdjacencyList AdjacencyList { get; private set; }

    public string PublicKey { get; private set; }

    public string SignedData
    {
        get => _signedData;
        private set
        {
            try
            {
                var decodedData = Convert.FromBase64String(value);
                var adjacencyList = Encoding.UTF8.GetString(decodedData[..^(Constants.DefaultPKeySize / 8)]);

                AdjacencyList = JsonSerializer.Deserialize<AdjacencyList>(adjacencyList) ?? throw new Exception();
                _signedData = value;
            }
            catch (Exception)
            {
                AdjacencyList = new([], string.Empty, null);
                _signedData = string.Empty;
            }
        }
    }

    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public VertexBroadcast(string publicKey, string signedData)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        PublicKey = publicKey;
        SignedData = signedData;
    }
}
