/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Enigma5.App.Common.Extensions;
using Enigma5.Security.Contracts;
using Enigma5.Crypto;

namespace Enigma5.App.Data;

[method: JsonConstructor]
public class Vertex(Neighborhood neighborhood, string? publicKey, string? signedData)
{
    [JsonIgnore]
    public DateTimeOffset LastUpdate { get; private set; }

    [JsonIgnore]
    public bool IsLeaf { get; private set; }

    [JsonIgnore]
    public bool PossibleLeaf => Neighborhood.Neighbors.Count == 1;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PublicKey { get; private set; } = publicKey;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignedData { get; private set; } = signedData;

    public Neighborhood Neighborhood { get; private set; } = neighborhood;

    public bool TryAsLeaf(out Vertex? leafVertex)
    {
        if (!PossibleLeaf)
        {
            leafVertex = null;
            return false;
        }

        var copy = this.CopyBySerialization();
        var neighborhood = new Neighborhood([.. copy.Neighborhood.Neighbors], copy.Neighborhood.Address, null);
        var leaf = new Vertex(neighborhood, null, null)
        {
            IsLeaf = true
        };
        leafVertex = leaf;
        return true;
    }

    public void RefreshLastUpdate() => LastUpdate = DateTimeOffset.Now;

    public static class Factory
    {
        public static Vertex Create(
        string publicKey,
        byte[] privateKey,
        string address,
        HashSet<string> neighbors,
        string? passphrase = null,
        string? hostname = null)
        {
            var neighborhood = new Neighborhood([.. neighbors], address, hostname);
            var serializedNeighborhood = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(neighborhood));
            using var envelope = Envelope.Factory.CreateSignature(privateKey, passphrase ?? string.Empty);
            var signature = envelope.Sign(serializedNeighborhood);

            return new Vertex(neighborhood, publicKey, Convert.ToBase64String(signature!));
        }

        public static Vertex Create(ICertificateManager certificateManager, HashSet<string> neighbors, string? hostname = null)
        => Create(
            certificateManager.PublicKey,
            certificateManager.PrivateKey,
            certificateManager.Address,
            neighbors,
            null,
            hostname);

        public static Vertex CreateWithEmptyNeighborhood(ICertificateManager certificateManager, string? hostname = null)
        => Create(certificateManager, [], hostname);

        public static Vertex Create(string address)
        => new(new([], address, null), string.Empty, null);

        public static class Prototype
        {
            public static bool AddNeighbors(Vertex vertex, List<string> addresses, ICertificateManager certificateManager, out Vertex? newVertex)
            {
                var previousCount = vertex.Neighborhood.Neighbors.Count;
                var neighborsToBeAdded = new HashSet<string>(addresses);
                var newNeighborsSet = vertex.Neighborhood.Neighbors.Union(neighborsToBeAdded).ToHashSet();

                if (previousCount != newNeighborsSet.Count)
                {
                    newVertex = Create(certificateManager, newNeighborsSet, vertex.Neighborhood.Hostname);
                    return true;
                }

                newVertex = null;
                return false;
            }

            public static bool AddNeighbor(Vertex vertex, string address, ICertificateManager certificateManager, out Vertex? newVertex)
            => AddNeighbors(vertex, [address], certificateManager, out newVertex);

            public static bool AddNeighbor(Vertex vertex, Vertex newNeighbor, ICertificateManager certificateManager, out Vertex? newVertex)
            => AddNeighbors(vertex, [newNeighbor.Neighborhood.Address], certificateManager, out newVertex);

            public static bool RemoveNeighbors(Vertex vertex, List<string> addresses, ICertificateManager certificateManager, out Vertex? newVertex)
            {
                var previousCount = vertex.Neighborhood.Neighbors.Count;
                var neighborsToBeRemoved = new HashSet<string>(addresses);
                var newNeighborsSet = vertex.Neighborhood.Neighbors.Except(neighborsToBeRemoved).ToHashSet();

                if (previousCount != newNeighborsSet.Count)
                {
                    newVertex = Create(certificateManager, newNeighborsSet, vertex.Neighborhood.Hostname);
                    return true;
                }

                newVertex = null;
                return false;
            }

            public static bool RemoveNeighbor(Vertex vertex, string address, ICertificateManager certificateManager, out Vertex? newVertex)
            => RemoveNeighbors(vertex, [address], certificateManager, out newVertex);

            public static bool RemoveNeighbor(Vertex vertex, Vertex neighbor, ICertificateManager certificateManager, out Vertex? newVertex)
            => RemoveNeighbors(vertex, [neighbor.Neighborhood.Address], certificateManager, out newVertex);
        }
    }

    public static bool operator ==(Vertex? obj1, Vertex? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        if (obj1.IsLeaf || obj2.IsLeaf)
        {
            return obj1.Neighborhood.Address == obj2.Neighborhood.Address
            && obj1.Neighborhood.Neighbors.SetEquals(obj2.Neighborhood.Neighbors);
        }

        return obj1.PublicKey == obj2.PublicKey
        && obj1.SignedData == obj2.SignedData
        && obj1.Neighborhood == obj2.Neighborhood;
    }

    public static bool operator !=(Vertex? obj1, Vertex? obj2) => !(obj1 == obj2);

    public override bool Equals(object? obj) => obj is Vertex other && Neighborhood.Address == other.Neighborhood.Address;

    public override int GetHashCode() => Neighborhood.Address.GetHashCode();
}
