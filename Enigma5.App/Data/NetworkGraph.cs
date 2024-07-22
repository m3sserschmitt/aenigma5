using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Security.Contracts;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Data;

public class NetworkGraph
{
    private readonly object _locker = new();

    private readonly ICertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private Vertex _localVertex;

    private readonly List<Vertex> _vertices;

    public List<Vertex> Vertices
    {
        get => ThreadSafeExecution.Execute(() => _vertices.CopyBySerialization(), [], _locker);
    }

    public List<string> Addresses
    {
        get => ThreadSafeExecution.Execute(() => _vertices.Select(item => item.Neighborhood.Address).ToList(), [], _locker);
    }

    public Vertex LocalVertex
    {
        get => ThreadSafeExecution.Execute(() => _localVertex.CopyBySerialization(), new Vertex(), _locker);
    }

    public bool HasAtLeastTwoVertices => _vertices.Any(item => !IsLocalVertex(item) && !item.IsLeaf);

    public List<string> NeighboringAddresses => [.. ThreadSafeExecution.Execute(() => _localVertex.CopyBySerialization().Neighborhood.Neighbors, [], _locker)];

    public NetworkGraph(ICertificateManager certificateManager, IConfiguration configuration)
    {
        _certificateManager = certificateManager;
        _configuration = configuration;
        _localVertex = Vertex.Factory.CreateWithEmptyNeighborhood(_certificateManager, _configuration.GetHostname());
        _vertices = [_localVertex];
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
                vertex = vertex.CopyBySerialization();
                var cleanupRequired = false;

                if (!IsLocalVertex(vertex))
                {
                    var updatedLocalVertex = UpdateLocalNeighborhood(vertex);

                    if (updatedLocalVertex)
                    {
                        updatedVertices.Add(_localVertex.CopyBySerialization());
                        cleanupRequired = true;
                    }

                    var previous = _vertices.FirstOrDefault(item => item.Neighborhood.Address == vertex.Neighborhood.Address);
                    previous?.RefreshLastUpdate();

                    if (previous is null)
                    {
                        _vertices.Add(TryConvertToLeaf(vertex));
                        updatedVertices.Add(vertex);
                    }
                    else if (previous != vertex)
                    {
                        _vertices.Remove(previous);
                        _vertices.Add(TryConvertToLeaf(vertex));
                        updatedVertices.Add(vertex);
                        cleanupRequired = true;
                    }

                    if (cleanupRequired)
                    {
                        CleanupGraph();
                    }
                }
                else
                {
                    ReplaceLocalVertex(vertex);
                    CleanupGraph();
                    updatedVertices.Add(_localVertex.CopyBySerialization());
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
    }

    private void CleanupGraph()
    {
        var leafsTimespan = _configuration.GetNonActiveLeafsLifetime() ?? new TimeSpan(24, 0, 0);

        var removalCandidates = new HashSet<string>(
            _vertices.Where(
                item => !item.IsLeaf || (item.IsLeaf && DateTimeOffset.Now - item.LastUpdate > leafsTimespan))
                .Select(item => item.Neighborhood.Address)
            );

        foreach (var vertex in _vertices)
        {
            foreach (var address in vertex.Neighborhood.Neighbors)
            {
                removalCandidates.Remove(address);
            }
        }

        _vertices.RemoveAll(
            item => item.Neighborhood.Address != _localVertex.Neighborhood.Address
            && removalCandidates.Contains(item.Neighborhood.Address));
    }

    private int LocalAdjacencyChanged(Vertex vertex2)
    => (_localVertex.Neighborhood.Neighbors.Contains(vertex2.Neighborhood.Address) ? 1 : 0)
       - (vertex2.Neighborhood.Neighbors.Contains(_localVertex.Neighborhood.Address) ? 1 : 0);
}
