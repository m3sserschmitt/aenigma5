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
using System.Text.Json.Serialization;
using Enigma5.Security.Contracts;
using Enigma5.Crypto;
using Enigma5.App.Common.Extensions;

namespace Enigma5.App.Data;

[method: JsonConstructor]
public class Vertex(Neighborhood neighborhood, string? publicKey, string? signedData)
{
    public string? PublicKey { get; private set; } = publicKey;

    public string? SignedData { get; private set; } = signedData;

    public Neighborhood Neighborhood { get; private set; } = neighborhood;

    public static class Factory
    {
        public static Vertex? Create(
        ICertificateManager certificateManager,
        HashSet<string> neighbors,
        string? hostname = null,
        string? onionService = null)
        {
            try
            {
                var publicKey = certificateManager.PublicKey;
                var neighborhood = new Neighborhood(neighbors, CertificateHelper.GetHexAddressFromPublicKey(publicKey), hostname, onionService, DateTimeOffset.Now);
                var serializedNeighborhood = Encoding.ASCII.GetBytes(neighborhood.CanonicallySerialize());
                var signature = certificateManager.CreateSigner().Sign(serializedNeighborhood);
                if(signature == null)
                {
                    return null;
                }
                return new Vertex(neighborhood, publicKey, Convert.ToBase64String(signature));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Vertex? Create(ICertificateManager certificateManager, Vertex vertex)
        => Create(certificateManager, vertex.Neighborhood.Neighbors, vertex.Neighborhood.Hostname, vertex.Neighborhood.OnionService);

        public static Vertex? Create(ICertificateManager certificateManager, string? hostname = null, string? onionService = null)
        => Create(certificateManager, [], hostname, onionService);

        public static Vertex Create(string address)
        => new(new([], address, null, null, DateTimeOffset.Now), null, null);

        public static class Prototype
        {
            public static bool AddNeighbors(Vertex vertex, List<string> addresses, ICertificateManager certificateManager, out Vertex? newVertex)
            {
                var previousCount = vertex.Neighborhood.Neighbors.Count;
                var neighborsToBeAdded = new HashSet<string>(addresses);
                var newNeighborsSet = vertex.Neighborhood.Neighbors.Union(neighborsToBeAdded).ToHashSet();

                if (previousCount != newNeighborsSet.Count)
                {
                    newVertex = Create(certificateManager, newNeighborsSet, vertex.Neighborhood.Hostname, vertex.Neighborhood.OnionService);
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
                    newVertex = Create(certificateManager, newNeighborsSet, vertex.Neighborhood.Hostname, vertex.Neighborhood.OnionService);
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

        return obj1.Neighborhood.Address == obj2.Neighborhood.Address;
    }

    public static bool operator !=(Vertex? obj1, Vertex? obj2) => !(obj1 == obj2);

    public override bool Equals(object? obj) => Equals(obj as Vertex);

    public bool Equals(Vertex? vertex) => this == vertex;

    public override int GetHashCode() => Neighborhood.Address.GetHashCode();
}
