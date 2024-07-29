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

        return obj1.Neighbors.SetEquals(obj2.Neighbors)
        && obj1.Hostname == obj2.Hostname
        && obj1.Address == obj2.Address;
    }

    public static bool operator !=(Neighborhood? obj1, Neighborhood? obj2) => !(obj1 == obj2);

    public override bool Equals(object? obj) => obj is Neighborhood other && this == other;

    public override int GetHashCode() => Address.GetHashCode();
}
