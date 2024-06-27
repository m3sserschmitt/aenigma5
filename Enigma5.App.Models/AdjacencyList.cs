using System.Text.Json;

namespace Enigma5.App.Models;

public class AdjacencyList
{
    public AdjacencyList()
    {
    }

    public AdjacencyList(List<string> neighbors, string address, string hostname)
    {
        Address = address;
        Hostname = hostname;
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
