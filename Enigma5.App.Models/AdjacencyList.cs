using System.Text.Json.Serialization;

namespace Enigma5.App.Models;

[method: JsonConstructor]
public class AdjacencyList(List<string> neighbors, string address, string? hostname)
{
    public string Address { get; set; } = address;

    public string? Hostname { get; set; } = hostname;

    public List<string> Neighbors { get; set; } = neighbors;
}
