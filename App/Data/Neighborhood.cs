namespace Enigma5.App.Data;

public class Neighborhood
{
    public Neighborhood()
    {
        Address = string.Empty;
        Neighbors = new();
    }
    
    public Neighborhood(List<string> neighbors, string address, string? hostname = null)
    {
        Neighbors = new HashSet<string>(neighbors);
        Address = address;
        Hostname = hostname;    
    }

    public string Address { get; set; }

    public string? Hostname { get; set; }

    public HashSet<string> Neighbors { get; set; }

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

        var list1 = obj1.Neighbors.OrderBy(x => x);
        var list2 = obj2.Neighbors.OrderBy(x => x);

        return list1.SequenceEqual(list2)
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
