﻿using System.Text.Json.Serialization;

namespace Enigma5.App.Data;

public class Neighborhood
{
    public Neighborhood()
    {
        Address = string.Empty;
        Neighbors = [];
        Hostname = null;
    }

    public Neighborhood(List<string> neighbors, string address, string? hostname = null)
    {
        Neighbors = new HashSet<string>(neighbors);
        Address = address;
        Hostname = hostname;
    }

    public string Address { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hostname { get; set; }

    public HashSet<string> Neighbors { get; set; }

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
