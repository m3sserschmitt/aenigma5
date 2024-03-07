using System.Text;
using System.Text.Json;
using Enigma5.Crypto;

namespace Enigma5.App.Models;

public class BroadcastAdjacencyList
{
    public BroadcastAdjacencyList()
    {
    }

    public BroadcastAdjacencyList(
        List<string> neighbors,
        string publicKey,
        string privateKey,
        string hostname)
    {
        _adjacencyList = new(neighbors, CertificateHelper.GetHexAddressFromPublicKey(publicKey), hostname);
        _signedData = Sign(privateKey);
        PublicKey = publicKey;
    }

    private AdjacencyList? _adjacencyList;

    private string? _signedData;

    public string? PublicKey { get; set; }

    public string? SignedData
    {
        get => _signedData;
        set
        {
            if (value == null)
            {
                _signedData = null;
                _adjacencyList = null;

                return;
            }

            try
            {
                var decodedData = Convert.FromBase64String(value);
                var adjacencyList = Encoding.UTF8.GetString(decodedData[..^(Constants.DefaultPKeySize / 8)]);

                _adjacencyList = JsonSerializer.Deserialize<AdjacencyList>(adjacencyList);
                _signedData = value;
            }
            catch
            {
                _adjacencyList = null;
                _signedData = null;
            }
        }
    }

    public AdjacencyList? GetAdjacencyList() => _adjacencyList;

    private string Sign(string privateKey)
    {
        var serializedList = JsonSerializer.Serialize(_adjacencyList);
        using var envelope = Envelope.Factory.CreateSignature(privateKey, string.Empty);
        var signature = envelope.Sign(Encoding.ASCII.GetBytes(serializedList))
        ?? throw new Exception("Could not sign the adjacency list");

        return Convert.ToBase64String(signature);
    }
}
