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
using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.UI;
using Enigma5.Crypto;
using Enigma5.Security.Contracts;

namespace Enigma5.App.Data;

public class NetworkGraph : IDisposable
{
    private bool _disposed;

    private readonly SimpleSingleThreadRunner _singleThreadRunner = new();

    private readonly ICertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private readonly NetworkGraphValidationPolicy _networkGraphValidationPolicy;

    private readonly ILogger<NetworkGraph> _logger;

    private Vertex _localVertex;

    private readonly HashSet<Vertex> _vertices;

    private readonly DashboardUIState _dashboardUIState;

    public NetworkGraph(
        ICertificateManager certificateManager,
        NetworkGraphValidationPolicy networkGraphValidationPolicy,
        IConfiguration configuration,
        ILogger<NetworkGraph> logger,
        DashboardUIState dashboardUIState)
    {
        _certificateManager = certificateManager;
        _configuration = configuration;
        _localVertex = Vertex.Factory.Create(null);
        _vertices = [_localVertex];
        _logger = logger;
        _dashboardUIState = dashboardUIState;
        _networkGraphValidationPolicy = networkGraphValidationPolicy;
    }

    ~NetworkGraph()
    {
        Dispose(false);
    }

    public Task<Vertex?> GetVertexAsync(string address) => _singleThreadRunner.RunAsync(() =>
    {
        var v = Vertex.Factory.Create(address);
        if (v is not null && _vertices.TryGetValue(v, out var foundVertex))
        {
            return foundVertex.CopyBySerialization();
        }

        return null;
    }, _logger);

    public Task<HashSet<Vertex>> GetVerticesAsync() => _singleThreadRunner.RunAsync(() => _vertices.CopyBySerialization(), _logger);

    public Task<Vertex> GetLocalVertexAsync() => _singleThreadRunner.RunAsync(() => _localVertex.CopyBySerialization(), _logger);

    public Task<HashSet<string>> GetNeighborAddressesAsync()
    => _singleThreadRunner.RunAsync(() => _localVertex.Neighborhood.Neighbors.CopyBySerialization(), _logger);

    public Task<string?> GetGraphHashAsync()
    => _singleThreadRunner.RunAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(_localVertex.SignedData))
            {
                return null;
            }
            var serializedGraph = _vertices.Select(v =>
                new Vertex(
                    new(v.Neighborhood.Neighbors, v.Neighborhood.Address, v.Neighborhood.Hostname, v.Neighborhood.OnionService, null),
                    v.PublicKey,
                    v.SignedData)
                ).OrderBy(v => v.Neighborhood.Address)
                .ToList()
                .CanonicallySerialize();
            return HashProvider.Sha256Hex(serializedGraph);
        }, _logger
    );

    public Task<Vertex> AddAdjacencyAsync(List<string> addresses)
    => _singleThreadRunner.RunAsync(async () =>
        {
            var newVertex = await Vertex.Factory.Prototype.AddNeighborsAsync(_localVertex, addresses, _certificateManager);
            if (newVertex != null && _networkGraphValidationPolicy.Validate(newVertex))
            {
                ReplaceLocalVertex(newVertex!);
                await NotifyPeersChangedAsync();

                return _localVertex.CopyBySerialization();
            }

            return _localVertex.CopyBySerialization();
        }, _logger
    );

    public Task<Vertex> RemoveAdjacencyAsync(List<string> addresses)
    => _singleThreadRunner.RunAsync(async () =>
        {
            var newVertex = await Vertex.Factory.Prototype.RemoveNeighborsAsync(_localVertex, addresses, _certificateManager);
            if (newVertex != null)
            {
                ReplaceLocalVertex(newVertex!);
                CleanupGraph();
                await NotifyPeersChangedAsync();

                return _localVertex.CopyBySerialization();
            }
            return _localVertex.CopyBySerialization();
        }, _logger
    );

    public Task<List<Vertex>> UpdateAsync(Vertex vertex) => _singleThreadRunner.RunAsync(async () =>
    {
        if (!_networkGraphValidationPolicy.Validate(vertex))
        {
            return [];
        }
        var updatedVertices = new List<Vertex>();

        if (IsLocalVertex(vertex))
        {
            return updatedVertices;
        }

        vertex = vertex.CopyBySerialization();

        if (await UpdateLocalNeighborhoodAsync(vertex))
        {
            updatedVertices.Add(_localVertex.CopyBySerialization());
        }

        if (!_vertices.TryGetValue(vertex, out var previous)) // vertex not existent;
        {
            _vertices.Add(vertex);
            await NotifyPeersChangedAsync();
            updatedVertices.Add(vertex);
        }
        else if (vertex.ShouldReplace(previous)) // existent but different;
        {
            _vertices.Remove(previous);
            _vertices.Add(vertex);
            updatedVertices.Add(vertex);
            CleanupGraph();
            await NotifyPeersChangedAsync();
        }

        return updatedVertices;
    }, _logger);

    public Task<bool> GenerateLocalVertexAsync()
    => _singleThreadRunner.RunAsync(async () =>
        {
            var v = await Vertex.Factory.CreateAsync(_certificateManager, _localVertex.Neighborhood.Neighbors, _configuration.GetHostname(), _configuration.GetOnionService());
            if (v is null)
            {
                ReplaceLocalVertex(Vertex.Factory.Create(await _certificateManager.GetAddressAsync()));
                return false;
            }
            ReplaceLocalVertex(v);
            return true;
        }, _logger);

    private async Task NotifyPeersChangedAsync()
    {
        var address = await _certificateManager.GetAddressAsync();
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }
        await _dashboardUIState.SetInboundPeersAsync([.. _vertices.Where(v => v.Neighborhood.Neighbors.Contains(address)).Select(v => new PeerDto {
            Host = v.Neighborhood.Hostname,
            Address = v.Neighborhood.Address,
            Connected = true
            })
        ]);
    }

    private bool IsLocalVertex(Vertex vertex)
    => vertex.Neighborhood.Address == _localVertex.Neighborhood.Address;

    private async Task<bool> UpdateLocalNeighborhoodAsync(Vertex source)
    {
        var result = LocalAdjacencyChanged(source);

        Vertex? newLocalVertex = null;

        if (result < 0)
        {
            newLocalVertex = await Vertex.Factory.Prototype.AddNeighborAsync(_localVertex, source, _certificateManager);
        }
        else if (result > 0)
        {
            newLocalVertex = await Vertex.Factory.Prototype.RemoveNeighborAsync(_localVertex, source, _certificateManager);
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
    => !IsLocalVertex(vertex) && (vertex.LastUpdateExceeded(vertexLifetime) || !neighborhoodsUnion.TryGetValue(vertex, out var _));

    private void CleanupGraph()
    {
        var vertexLifetime = _configuration.GetVertexLifetime();
        var neighborhoodsUnion = GetNeighborhoodsUnion();
        _vertices.RemoveWhere(item => IsRemovalCandidate(item, neighborhoodsUnion, vertexLifetime));
    }

    private int LocalAdjacencyChanged(Vertex vertex2)
    {
        if (string.IsNullOrWhiteSpace(vertex2.Neighborhood.Address) || string.IsNullOrWhiteSpace(_localVertex.Neighborhood.Address))
        {
            return 0;
        }
        return (_localVertex.Neighborhood.Neighbors.Contains(vertex2.Neighborhood.Address) ? 1 : 0)
       - (vertex2.Neighborhood.Neighbors.Contains(_localVertex.Neighborhood.Address) ? 1 : 0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {

            }
            _singleThreadRunner.Dispose();
            _disposed = true;
        }
    }
}
