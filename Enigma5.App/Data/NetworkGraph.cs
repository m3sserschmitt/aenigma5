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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.UI;
using Enigma5.Security.Contracts;

namespace Enigma5.App.Data;

public class NetworkGraph
{
    private readonly object _locker = new();

    private readonly ICertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private readonly ILogger<NetworkGraph> _logger;

    private Vertex _localVertex;

    private readonly HashSet<Vertex> _vertices;

    private readonly DashboardUIState _dashboardUIState;

    public HashSet<Vertex> Vertices => ThreadSafeExecution.Execute(() => _vertices.CopyBySerialization(), [], _locker, _logger);

    public Vertex? LocalVertex => ThreadSafeExecution.Execute(() => _localVertex.CopyBySerialization(), null, _locker, _logger);

    virtual public HashSet<string> NeighboringAddresses => ThreadSafeExecution.Execute(() => _localVertex.Neighborhood.Neighbors.CopyBySerialization(), [], _locker, _logger);

    public NetworkGraph(ICertificateManager certificateManager, IConfiguration configuration, ILogger<NetworkGraph> logger, DashboardUIState dashboardUIState)
    {
        _certificateManager = certificateManager;
        _configuration = configuration;
        _localVertex = Vertex.Factory.Create(certificateManager.Address);
        _vertices = [_localVertex];
        _logger = logger;
        _dashboardUIState = dashboardUIState;
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
    => Task.Run(() => GetVertex(address), cancellationToken);

    public Vertex AddAdjacency(List<string> addresses)
    => ThreadSafeExecution.Execute(
        () =>
        {
            if (Vertex.Factory.Prototype.AddNeighbors(_localVertex, addresses, _certificateManager, out Vertex? newVertex)
                && (newVertex?.ValidatePolicy() ?? false))
            {
                ReplaceLocalVertex(newVertex!);
                NotifyPeersChanged();

                return _localVertex.CopyBySerialization();
            }

            return _localVertex.CopyBySerialization();
        },
        _localVertex.CopyBySerialization(),
        _locker,
        _logger
    );

    public Vertex RemoveAdjacency(List<string> addresses)
    => ThreadSafeExecution.Execute(
        () =>
        {
            if (Vertex.Factory.Prototype.RemoveNeighbors(_localVertex, addresses, _certificateManager, out Vertex? newVertex))
            {
                ReplaceLocalVertex(newVertex!);
                CleanupGraph();
                NotifyPeersChanged();

                return _localVertex.CopyBySerialization();
            }
            return _localVertex.CopyBySerialization();
        },
        _localVertex.CopyBySerialization(),
        _locker,
        _logger
    );

    public Task<Vertex> AddAdjacencyAsync(List<string> addresses, CancellationToken cancellationToken = default)
    => Task.Run(() => AddAdjacency(addresses), cancellationToken);

    public Task<Vertex> RemoveAdjacencyAsync(List<string> addresses, CancellationToken cancellationToken = default)
    => Task.Run(() => RemoveAdjacency(addresses), cancellationToken);

    public List<Vertex> Update(Vertex vertex)
    {
        if (!vertex.ValidatePolicy())
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

                if (!_vertices.TryGetValue(vertex, out var previous)) // vertex not existent;
                {
                    _vertices.Add(vertex);
                    NotifyPeersChanged();
                    updatedVertices.Add(vertex);
                }
                else if (vertex.ShouldReplace(previous)) // existent but different;
                {
                    _vertices.Remove(previous);
                    _vertices.Add(vertex);
                    updatedVertices.Add(vertex);
                    CleanupGraph();
                    NotifyPeersChanged();
                }

                return updatedVertices;
            },
            [],
            _locker,
            _logger
        );
    }

    public Task<List<Vertex>> UpdateAsync(Vertex vertex, CancellationToken cancellationToken = default)
    => Task.Run(() => Update(vertex), cancellationToken);

    public bool GenerateLocalVertex()
    => ThreadSafeExecution.Execute(() =>
        {
            var v = Vertex.Factory.Create(_certificateManager, _localVertex.Neighborhood.Neighbors, _configuration.GetHostname(), _configuration.GetOnionService());
            if (v is null)
            {
                return false;
            }
            ReplaceLocalVertex(v);
            return true;
        }, false, _locker, _logger);

    public Task<bool> GenerateLocalVertexAsync(CancellationToken cancellationToken = default) => Task.Run(GenerateLocalVertex, cancellationToken);

    private void NotifyPeersChanged()
    {
        _dashboardUIState.Peers = [.. _vertices.Where(v => v.Neighborhood.Neighbors.Contains(_certificateManager.Address)).Select(v => new PeerDto {
            Host = v.Neighborhood.Hostname,
            Address = v.Neighborhood.Address
            })
        ];
    }

    private bool IsLocalVertex(Vertex vertex)
    => vertex.Neighborhood.Address == _localVertex.Neighborhood.Address;

    private bool UpdateLocalNeighborhood(Vertex source)
    {
        var result = LocalAdjacencyChanged(source);

        Vertex? newLocalVertex = null;

        if (result < 0)
        {
            Vertex.Factory.Prototype.AddNeighbor(_localVertex, source, _certificateManager, out newLocalVertex);
        }
        else if (result > 0)
        {
            Vertex.Factory.Prototype.RemoveNeighbor(_localVertex, source, _certificateManager, out newLocalVertex);
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

        foreach (var vertex in _vertices)
        {
            union.UnionWith(vertex.Neighborhood.Neighbors.Select(Vertex.Factory.Create).Where(item => item is not null)!);
        }

        return union;
    }

    private bool IsRemovalCandidate(Vertex vertex, HashSet<Vertex> neighborhoodsUnion, TimeSpan vertexLifetime)
    => !IsLocalVertex(vertex) && (vertex.IsExpired(vertexLifetime) || !neighborhoodsUnion.TryGetValue(vertex, out var _));

    private void CleanupGraph()
    {
        var vertexLifetime = _configuration.GetVertexLifetime();
        var neighborhoodsUnion = GetNeighborhoodsUnion();
        _vertices.RemoveWhere(item => IsRemovalCandidate(item, neighborhoodsUnion, vertexLifetime));
    }

    private int LocalAdjacencyChanged(Vertex vertex2)
    => (_localVertex.Neighborhood.Neighbors.Contains(vertex2.Neighborhood.Address) ? 1 : 0)
       - (vertex2.Neighborhood.Neighbors.Contains(_localVertex.Neighborhood.Address) ? 1 : 0);
}
