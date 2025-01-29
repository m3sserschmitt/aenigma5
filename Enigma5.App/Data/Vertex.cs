/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.Security.Contracts;
using Enigma5.Crypto.Contracts;
using Enigma5.Crypto;

namespace Enigma5.App.Data;

[method: JsonConstructor]
public class Vertex(Neighborhood neighborhood, string? publicKey, string? signedData, bool isLeaf = false)
{
    public DateTimeOffset LastUpdate { get; private set; } = DateTimeOffset.Now;

    public bool IsLeaf { get; private set; } = isLeaf;

    public bool PossibleLeaf => Neighborhood.Neighbors.Count == 1 && Neighborhood.Hostname is null;

    public string? PublicKey { get; private set; } = publicKey;

    public string? SignedData { get; private set; } = signedData;

    public Neighborhood Neighborhood { get; private set; } = neighborhood;

    public bool TryAsLeaf(out Vertex? leafVertex, bool addSignedData)
    {
        if (!PossibleLeaf)
        {
            leafVertex = null;
            return false;
        }

        var neighborhood = new Neighborhood(Neighborhood.Neighbors, Neighborhood.Address, null);
        leafVertex = new Vertex(neighborhood, null, addSignedData ? SignedData : null, true);

        return true;
    }

    public void RefreshLastUpdate() => LastUpdate = DateTimeOffset.Now;

    public static class Factory
    {
        public static Vertex? Create(
        string publicKey,
        IEnvelopeSigner signer,
        HashSet<string> neighbors,
        string? hostname = null)
        {
            try
            {
                var neighborhood = new Neighborhood(neighbors, CertificateHelper.GetHexAddressFromPublicKey(publicKey), hostname);
                var serializedNeighborhood = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(neighborhood));
                var signature = signer.Sign(serializedNeighborhood);

                return new Vertex(neighborhood, publicKey, Convert.ToBase64String(signature!));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Vertex? Create(IEnvelopeSigner signer, ICertificateManager certificateManager, HashSet<string> neighbors, string? hostname = null)
        => Create(
            certificateManager.PublicKey,
            signer,
            neighbors,
            hostname);

        public static Vertex? CreateWithEmptyNeighborhood(IEnvelopeSigner signer, ICertificateManager certificateManager, string? hostname = null)
        => Create(signer, certificateManager, [], hostname);

        public static Vertex? Create(string address)
        => new(new([], address, null), string.Empty, null);

        public static class Prototype
        {
            public static bool AddNeighbors(Vertex vertex, List<string> addresses, IEnvelopeSigner signer, ICertificateManager certificateManager, out Vertex? newVertex)
            {
                var previousCount = vertex.Neighborhood.Neighbors.Count;
                var neighborsToBeAdded = new HashSet<string>(addresses);
                var newNeighborsSet = vertex.Neighborhood.Neighbors.Union(neighborsToBeAdded).ToHashSet();

                if (previousCount != newNeighborsSet.Count)
                {
                    newVertex = Create(signer, certificateManager, newNeighborsSet, vertex.Neighborhood.Hostname);
                    return true;
                }

                newVertex = null;
                return false;
            }

            public static bool AddNeighbor(Vertex vertex, string address, IEnvelopeSigner signer, ICertificateManager certificateManager, out Vertex? newVertex)
            => AddNeighbors(vertex, [address], signer, certificateManager, out newVertex);

            public static bool AddNeighbor(Vertex vertex, Vertex newNeighbor, IEnvelopeSigner signer, ICertificateManager certificateManager, out Vertex? newVertex)
            => AddNeighbors(vertex, [newNeighbor.Neighborhood.Address], signer, certificateManager, out newVertex);

            public static bool RemoveNeighbors(Vertex vertex, List<string> addresses, IEnvelopeSigner signer, ICertificateManager certificateManager, out Vertex? newVertex)
            {
                var previousCount = vertex.Neighborhood.Neighbors.Count;
                var neighborsToBeRemoved = new HashSet<string>(addresses);
                var newNeighborsSet = vertex.Neighborhood.Neighbors.Except(neighborsToBeRemoved).ToHashSet();

                if (previousCount != newNeighborsSet.Count)
                {
                    newVertex = Create(signer, certificateManager, newNeighborsSet, vertex.Neighborhood.Hostname);
                    return true;
                }

                newVertex = null;
                return false;
            }

            public static bool RemoveNeighbor(Vertex vertex, string address, IEnvelopeSigner signer, ICertificateManager certificateManager, out Vertex? newVertex)
            => RemoveNeighbors(vertex, [address], signer, certificateManager, out newVertex);

            public static bool RemoveNeighbor(Vertex vertex, Vertex neighbor, IEnvelopeSigner signer, ICertificateManager certificateManager, out Vertex? newVertex)
            => RemoveNeighbors(vertex, [neighbor.Neighborhood.Address], signer, certificateManager, out newVertex);
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
