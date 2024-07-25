using System.Text.Json.Serialization;
using Enigma5.App.Models;

namespace Enigma5.App.Data;

[method: JsonConstructor]
public class Neighborhood(HashSet<string> neighbors, string address, string? hostname)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hostname { get; private set; } = hostname;

    public string Address { get; private set; } = address;

    public HashSet<string> Neighbors { get; private set; } = new HashSet<string>(neighbors);

    public static Neighborhood FromAdjacency(AdjacencyList adjacencyList)
    => new([.. adjacencyList.Neighbors], adjacencyList.Address ?? string.Empty, adjacencyList.Hostname);

    public bool CompareNeighbors(Neighborhood other)
    {
        var list1 = other.Neighbors.OrderBy(x => x);
        var list2 = Neighbors.OrderBy(x => x);

        return list1.SequenceEqual(list2);
    }

    public static bool operator ==(Neighborhood? obj1, Neighborhood? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        return obj1.CompareNeighbors(obj2)
        && obj1.Hostname == obj2.Hostname
        && obj1.Address == obj2.Address;
    }

    public static bool operator !=(Neighborhood? obj1, Neighborhood? obj2)
    {
        return !(obj1 == obj2);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Neighborhood other)
        {
            return this == other;
        }
        return false;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
