using System.Text;
using System.Text.Json;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Security.Contracts;
using Enigma5.Crypto;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Data;

public class NetworkGraph
{
    private readonly object _locker = new();

    private readonly ICertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private Vertex _localVertex;

    private readonly HashSet<Vertex> _vertices;

    private Graph _graph;

    public Graph Graph => ThreadSafeExecution.Execute(() => _graph.CopyBySerialization(), new Graph(_certificateManager.PublicKey, string.Empty), _locker);

    public HashSet<Vertex> Vertices => ThreadSafeExecution.Execute(() => _vertices.CopyBySerialization(), [], _locker);

    public Vertex LocalVertex => ThreadSafeExecution.Execute(() => _localVertex.CopyBySerialization(), CreateInitialVertex(), _locker);

    public bool HasAtLeastTwoVertices => ThreadSafeExecution.Execute(() => _vertices.Any(item => !IsLocalVertex(item) && !item.IsLeaf), false, _locker);

    public List<string> NeighboringAddresses => [.. ThreadSafeExecution.Execute(() => _localVertex.CopyBySerialization().Neighborhood.Neighbors, [], _locker)];

    public NetworkGraph(ICertificateManager certificateManager, IConfiguration configuration)
    {
        _certificateManager = certificateManager;
        _configuration = configuration;
        _localVertex = CreateInitialVertex();
        _vertices = [_localVertex];
        SignGraph();
        _graph ??= new Graph(_certificateManager.PublicKey, string.Empty);
    }

    public (Vertex localVertex, bool updated) AddAdjacency(string address)
    => ThreadSafeExecution.Execute(
        () =>
        {
            if (Vertex.Factory.Prototype.AddNeighbor(_localVertex, address, _certificateManager, out Vertex? newVertex)
                && ValidateNewVertex(newVertex!))
            {
                ReplaceLocalVertex(newVertex!);
                return (_localVertex.CopyBySerialization(), true);
            }

            return (_localVertex.CopyBySerialization(), false);
        },
        (_localVertex.CopyBySerialization(), false),
        _locker
    );

    public (Vertex localVertex, bool updated) RemoveAdjacency(string address)
    => ThreadSafeExecution.Execute(
        () =>
        {
            if (Vertex.Factory.Prototype.RemoveNeighbor(_localVertex, address, _certificateManager, out Vertex? newVertex))
            {
                ReplaceLocalVertex(newVertex!);
                CleanupGraph();

                return (_localVertex.CopyBySerialization(), true);
            }
            return (_localVertex.CopyBySerialization(), false);
        },
        (_localVertex.CopyBySerialization(), false),
        _locker
    );

    public Task<(Vertex localVertex, bool updated)> AddAdjacencyAsync(string address, CancellationToken cancellationToken = default)
    {
        (Vertex, bool) task() => AddAdjacency(address);
        return Task.Run(task, cancellationToken);
    }

    public Task<(Vertex localVertex, bool updated)> RemoveAdjacencyAsync(string address, CancellationToken cancellationToken = default)
    {
        (Vertex, bool) task() => RemoveAdjacency(address);
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

                var updatedLocalVertex = UpdateLocalNeighborhood(vertex);
                if (updatedLocalVertex)
                {
                    updatedVertices.Add(_localVertex.CopyBySerialization());
                }

                if (!_vertices.TryGetValue(vertex, out var previous)) // vertex not existent;
                {
                    _vertices.Add(TryConvertToLeaf(vertex));
                    updatedVertices.Add(vertex);
                    SignGraph();
                }
                else if (previous != vertex) // existent but different;
                {
                    _vertices.Remove(previous!);
                    _vertices.Add(TryConvertToLeaf(vertex));
                    updatedVertices.Add(vertex);
                    CleanupGraph();
                    SignGraph();
                }
                else // existent but and it is the same;
                {
                    previous?.RefreshLastUpdate();
                }

                return updatedVertices;
            },
            [],
            _locker
        );
    }

    public Task<List<Vertex>> UpdateAsync(Vertex vertex, CancellationToken cancellationToken = default)
    {
        List<Vertex> task() => Update(vertex);
        return Task.Run(task, cancellationToken);
    }

    private void SignGraph()
    {
        using var envelope = Envelope.Factory.CreateSignature(_certificateManager.PrivateKey, string.Empty);
        var serializedData = JsonSerializer.Serialize(_vertices.Where(item => !item.IsLeaf));
        var signature = envelope.Sign(Encoding.UTF8.GetBytes(serializedData));
        var encodedSignature = signature is not null ? Convert.ToBase64String(signature) : null;

        if(encodedSignature is null)
        {
            // TODO: Log this!!
            return;
        }

        
        _graph = new Graph(_certificateManager.PublicKey, encodedSignature);
    }

    private Vertex CreateInitialVertex() => Vertex.Factory.CreateWithEmptyNeighborhood(_certificateManager, _configuration.GetHostname());

    private Vertex TryConvertToLeaf(Vertex vertex)
    {
        if (HasAtLeastTwoVertices && vertex.TryAsLeaf(out var leafVertex))
        {
            return leafVertex!;
        }

        return vertex;
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
            _locker
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
        if (HasAtLeastTwoVertices && source.PossibleLeaf)
        {
            return false;
        }

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
        SignGraph();
    }

    private HashSet<Vertex> GetNeighborhoodsUnion()
    {
        var union = new HashSet<Vertex>(_vertices.Count);

        foreach (var vertex in _vertices.Where(item => !item.IsLeaf))
        {
            union.UnionWith(vertex.Neighborhood.Neighbors.Select(Vertex.Factory.Create));
        }

        return union;
    }

    private bool IsRemovalCandidate(Vertex vertex, HashSet<Vertex> neighborhoodsUnion, TimeSpan leafsTimeSpan)
    => !IsLocalVertex(vertex) && vertex.IsRemovalCandidate(leafsTimeSpan) && !neighborhoodsUnion.TryGetValue(vertex, out var _);

    private void CleanupGraph()
    {
        var leafsTimeSpan = _configuration.GetNonActiveLeafsLifetime() ?? new TimeSpan(24, 0, 0);
        var neighborhoodsUnion = GetNeighborhoodsUnion();
        _vertices.RemoveWhere(item => IsRemovalCandidate(item, neighborhoodsUnion, leafsTimeSpan));
    }

    private int LocalAdjacencyChanged(Vertex vertex2)
    => (_localVertex.Neighborhood.Neighbors.Contains(vertex2.Neighborhood.Address) ? 1 : 0)
       - (vertex2.Neighborhood.Neighbors.Contains(_localVertex.Neighborhood.Address) ? 1 : 0);
}
