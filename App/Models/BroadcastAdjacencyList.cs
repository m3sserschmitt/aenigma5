using System.Text;
using System.Text.Json;
using Enigma5.App.Security;
using Enigma5.Crypto;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Models;

public class BroadcastAdjacencyList
{
    public BroadcastAdjacencyList()
    {
    }

    public BroadcastAdjacencyList(List<string> neighbors, CertificateManager certificateManager, IConfiguration configuration)
    {
        AdjacencyList = new(neighbors, certificateManager, configuration);
        PublicKey = certificateManager.PublicKey;
        Signature = Sign(certificateManager);
    }

    public AdjacencyList? AdjacencyList { get; set; }

    public string? PublicKey { get; set; }

    public string? Signature { get; set; }

    private string Sign(CertificateManager certificateManager)
    {
        var serializedList = JsonSerializer.Serialize(AdjacencyList);
        var signature = Envelope.Factory.CreateSignature(certificateManager.PrivateKey, string.Empty)
                .Sign(Encoding.ASCII.GetBytes(serializedList)) ?? throw new Exception("Could not sign the adjacency list");

        return Convert.ToBase64String(signature);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
