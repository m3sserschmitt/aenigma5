/*
    Aenigma - Federal messaging system
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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Data.Extensions;
using Enigma5.Crypto.Contracts;
using Enigma5.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Data;

public class NetworkGraph
{
    private readonly object _locker = new();

    private readonly IEnvelopeSigner _signer;

    private readonly ICertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private readonly ILogger<NetworkGraph> _logger;

    private Vertex _localVertex;

    private readonly HashSet<Vertex> _vertices;

    public HashSet<Vertex> Vertices => ThreadSafeExecution.Execute(() => _vertices.CopyBySerialization(), [], _locker, _logger);

    public Vertex? LocalVertex => ThreadSafeExecution.Execute(() => _localVertex.CopyBySerialization(), null, _locker, _logger);

    virtual public HashSet<string> NeighboringAddresses => ThreadSafeExecution.Execute(() => _localVertex.Neighborhood.Neighbors.CopyBySerialization(), [], _locker, _logger);

    public HashSet<Vertex> NonLeafVertices => ThreadSafeExecution.Execute(() => _vertices.Where(item => !item.IsLeaf).Select(item => item.CopyBySerialization()).ToHashSet(), [], _locker, _logger);

    public NetworkGraph(IEnvelopeSigner signer, ICertificateManager certificateManager, IConfiguration configuration, ILogger<NetworkGraph> logger)
    {
        _signer = signer;
        _certificateManager = certificateManager;
        _configuration = configuration;
        _localVertex = CreateInitialVertex();
        _vertices = [_localVertex];
        _logger = logger;
    }

    public Vertex? GetVertex(string address)
    => ThreadSafeExecution.Execute(
        () =>
        {
            var v = Vertex.Factory.Create(address);
            if (v is not null && _vertices.TryGetValue(v, out var foundVertex))
            {
                return foundVertex.CopyBySerialization();
            }

            return null;
        }, null, _locker, _logger
    );

    public Task<Vertex?> GetVertexAsync(string address, CancellationToken cancellationToken = default)
    {
        Vertex? task() => GetVertex(address);
        return Task.Run(task, cancellationToken);
    }

    public (Vertex localVertex, bool updated) AddAdjacency(List<string> addresses)
    => ThreadSafeExecution.Execute(
        () =>
        {
            if (Vertex.Factory.Prototype.AddNeighbors(_localVertex, addresses, _signer, _certificateManager, out Vertex? newVertex)
                && ValidateNewVertex(newVertex!))
            {
                ReplaceLocalVertex(newVertex!);

                return (_localVertex.CopyBySerialization(), true);
            }

            return (_localVertex.CopyBySerialization(), false);
        },
        (_localVertex.CopyBySerialization(), false),
        _locker,
        _logger
    );

    public (Vertex localVertex, bool updated) RemoveAdjacency(List<string> addresses)
    => ThreadSafeExecution.Execute(
        () =>
        {
            if (Vertex.Factory.Prototype.RemoveNeighbors(_localVertex, addresses, _signer, _certificateManager, out Vertex? newVertex))
            {
                ReplaceLocalVertex(newVertex!);
                CleanupGraph();

                return (_localVertex.CopyBySerialization(), true);
            }
            return (_localVertex.CopyBySerialization(), false);
        },
        (_localVertex.CopyBySerialization(), false),
        _locker,
        _logger
    );

    public Task<(Vertex localVertex, bool updated)> AddAdjacencyAsync(List<string> addresses, CancellationToken cancellationToken = default)
    {
        (Vertex, bool) task() => AddAdjacency(addresses);
        return Task.Run(task, cancellationToken);
    }

    public Task<(Vertex localVertex, bool updated)> RemoveAdjacencyAsync(List<string> addresses, CancellationToken cancellationToken = default)
    {
        (Vertex, bool) task() => RemoveAdjacency(addresses);
        return Task.Run(task, cancellationToken);
    }

    public List<Vertex> Update(Vertex vertex)
    {
        if (!ValidateNewVertex(vertex))
        {
            return [];
        }

        return ThreadSafeExecution.Execute(
            () =>
            {
                var updatedVertices = new List<Vertex>();

                if (IsLocalVertex(vertex))
                {
                    return updatedVertices;
                }

                vertex = vertex.CopyBySerialization();

                if (UpdateLocalNeighborhood(vertex))
                {
                    updatedVertices.Add(_localVertex.CopyBySerialization());
                }

                var vertexToBeAdded = vertex.TryAsLeaf(out var leaf) ? leaf! : vertex;
                if (!_vertices.TryGetValue(vertex, out var previous)) // vertex not existent;
                {
                    _vertices.Add(vertexToBeAdded);
                    updatedVertices.Add(vertex);
                }
                else if (previous != vertex) // existent but different;
                {
                    _vertices.Remove(previous!);
                    _vertices.Add(vertexToBeAdded);
                    updatedVertices.Add(vertex);
                    CleanupGraph();
                }
                else if (previous.ShallBeBroadcasted()) // existent and it is the same;
                {
                    updatedVertices.Add(previous);
                    previous.RefreshLastUpdate();
                }

                return updatedVertices;
            },
            [],
            _locker,
            _logger
        );
    }

    public Task<List<Vertex>> UpdateAsync(Vertex vertex, CancellationToken cancellationToken = default)
    {
        List<Vertex> task() => Update(vertex);
        return Task.Run(task, cancellationToken);
    }

    private Vertex CreateInitialVertex()
    { 
        var v = Vertex.Factory.CreateWithEmptyNeighborhood(_signer, _certificateManager, _configuration.GetHostname());
        if(v is null)
        {
            var ex = new Exception("Initial vertex resolved to null.");
            _logger.LogCritical(ex, "Critical error occurred while creating initial vertex.");
            throw ex;
        }
        return v;
    }

    private bool ValidateNewVertex(Vertex vertex)
    {
        if (!vertex.ValidatePolicy())
        {
            return false;
        }

        return ThreadSafeExecution.Execute(
            () => ValidateGraphPolicy(vertex),
            false,
            _locker,
            _logger
        );
    }

    private bool ValidateGraphPolicy(Vertex vertex)
    => IsLocalVertex(vertex) || NeighborsExistsInGraph(vertex);

    private bool NeighborsExistsInGraph(Vertex vertex)
    {
        var nonLeafVertices = new HashSet<string>(
            _vertices.Where(item => !item.IsLeaf).Select(item => item.Neighborhood.Address)
        );
        return vertex.Neighborhood.Neighbors.All(item => !nonLeafVertices.Add(item));
    }

    private bool IsLocalVertex(Vertex vertex)
    => vertex.Neighborhood.Address == _localVertex.Neighborhood.Address;

    private bool UpdateLocalNeighborhood(Vertex source)
    {
        if (source.PossibleLeaf)
        {
            return false;
        }

        var result = LocalAdjacencyChanged(source);

        Vertex? newLocalVertex = null;

        if (result < 0)
        {
            Vertex.Factory.Prototype.AddNeighbor(_localVertex, source, _signer, _certificateManager, out newLocalVertex);
        }
        else if (result > 0)
        {
            Vertex.Factory.Prototype.RemoveNeighbor(_localVertex, source, _signer, _certificateManager, out newLocalVertex);
        }

        if (result == 0)
        {
            return false;
        }

        ReplaceLocalVertex(newLocalVertex!);

        return true;
    }

    private void ReplaceLocalVertex(Vertex vertex)
    {
        _vertices.Remove(_localVertex);
        _localVertex = vertex.CopyBySerialization();
        _vertices.Add(_localVertex);
    }

    private HashSet<Vertex> GetNeighborhoodsUnion()
    {
        var union = new HashSet<Vertex>(_vertices.Count);

        foreach (var vertex in _vertices.Where(item => !item.IsLeaf))
        {
            union.UnionWith(vertex.Neighborhood.Neighbors.Select(Vertex.Factory.Create).Where(item => item is not null)!);
        }

        return union;
    }

    private bool IsRemovalCandidate(Vertex vertex, HashSet<Vertex> neighborhoodsUnion, TimeSpan leafsLifetime)
    => !IsLocalVertex(vertex) && vertex.IsRemovalCandidate(leafsLifetime) && !neighborhoodsUnion.TryGetValue(vertex, out var _);

    private void CleanupGraph()
    {
        var leafsLifetime = _configuration.GetLeafsLifetime();
        var neighborhoodsUnion = GetNeighborhoodsUnion();
        _vertices.RemoveWhere(item => IsRemovalCandidate(item, neighborhoodsUnion, leafsLifetime));
    }

    private int LocalAdjacencyChanged(Vertex vertex2)
    => (_localVertex.Neighborhood.Neighbors.Contains(vertex2.Neighborhood.Address) ? 1 : 0)
       - (vertex2.Neighborhood.Neighbors.Contains(_localVertex.Neighborhood.Address) ? 1 : 0);
}
