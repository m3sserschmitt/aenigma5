using System.Text.Json;
using Enigma5.App.Security;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Models;

public class AdjacencyList
{
    public AdjacencyList()
    {
    }

    public AdjacencyList(List<string> neighbors, CertificateManager certificateManager, IConfiguration configuration)
    {
        Address = certificateManager.Address;
        Hostname = configuration.GetValue<string>("Hostname");
        Neighbors = neighbors;
    }

    public string? Address { get; set; }

    public string? Hostname { get; set; }

    public List<string>? Neighbors { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
