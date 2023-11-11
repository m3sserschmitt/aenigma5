using System.Text;
using System.Text.Json;
using Enigma5.App.Security;
using Enigma5.Core;
using Enigma5.Crypto;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Models;

public class BroadcastAdjacencyList
{
    public BroadcastAdjacencyList()
    {
    }

    public BroadcastAdjacencyList(
        List<string> neighbors,
        CertificateManager certificateManager,
        IConfiguration configuration)
    {
        _adjacencyList = new(neighbors, certificateManager, configuration);
        _signedData = Sign(certificateManager);
        PublicKey = certificateManager.PublicKey;
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
                var adjacencyList = Encoding.UTF8.GetString(decodedData[..^(PKeyContext.Current.PKeySize / 8)]);

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

    private string Sign(CertificateManager certificateManager)
    {
        var serializedList = JsonSerializer.Serialize(_adjacencyList);
        using var envelope = Envelope.Factory.CreateSignature(certificateManager.PrivateKey, string.Empty);
        var signature = envelope.Sign(Encoding.ASCII.GetBytes(serializedList))
        ?? throw new Exception("Could not sign the adjacency list");

        return Convert.ToBase64String(signature);
    }
}
