using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Security;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Data;

public class NetworkGraph
{
    private readonly object _locker = new();

    private readonly CertificateManager _certificateManager;

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

    public NetworkGraph(CertificateManager certificateManager, IConfiguration configuration)
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
                && NetworkGraphValidationPolicy.Validate(newVertex!))
            {
                ReplaceLocalVertex(newVertex!);
                return (LocalVertex, true);
            }

            return (LocalVertex, false);
        },
        (LocalVertex, false),
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

                return (LocalVertex, true);
            }
            return (LocalVertex, false);
        },
        (LocalVertex, false),
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

    public (IList<Vertex> vertices, Delta delta) Update(Vertex vertex)
    {
        if (!NetworkGraphValidationPolicy.Validate(vertex))
        {
            return ([], new());
        }

        return ThreadSafeExecution.Execute(
            () =>
            {
                var vertices = new List<Vertex>();
                Delta delta = new();

                if (!IsLocalVertex(vertex))
                {
                    (bool updated, delta) = UpdateLocalNeighborhood(vertex);

                    if (updated)
                    {
                        vertices.Add(_localVertex);
                    }

                    var previous = _vertices.FirstOrDefault(item => item.Neighborhood.Address == vertex.Neighborhood.Address);

                    if (previous == null)
                    {
                        _vertices.Add(vertex);
                        vertices.Add(vertex);
                    }
                    else if (previous != vertex)
                    {
                        _vertices.Remove(previous);
                        _vertices.Add(vertex);
                        vertices.Add(vertex);
                    }

                    if (updated || previous != vertex)
                    {
                        CleanupGraph();
                    }
                }
                else
                {
                    ReplaceLocalVertex(vertex);
                    CleanupGraph();
                    vertices.Add(_localVertex);
                }

                return (vertices, delta);
            },
            ([], new()),
            _locker
        );
    }

    public Task<(IList<Vertex> vertices, Delta delta)> UpdateAsync(Vertex vertex, CancellationToken cancellationToken = default)
    {
        (IList<Vertex>, Delta delta) task() => Update(vertex);
        return Task.Run(task, cancellationToken);
    }

    private bool IsLocalVertex(Vertex vertex)
    => vertex.Neighborhood.Address == _localVertex.Neighborhood.Address;

    private (bool updated, Delta delta) UpdateLocalNeighborhood(Vertex source)
    {
        var result = LocalAdjacencyChanged(source);
        bool added = false;

        Vertex? newLocalVertex = null;

        if (result < 0)
        {
            Vertex.Factory.Prototype.AddNeighbor(_localVertex, source, _certificateManager, out newLocalVertex);
            added = true;
        }
        else if (result > 0)
        {
            Vertex.Factory.Prototype.RemoveNeighbor(_localVertex, source, _certificateManager, out newLocalVertex);
        }

        if (result == 0)
        {
            return (false, new());
        }

        ReplaceLocalVertex(newLocalVertex!);

        return (true, new Delta(source, added));
    }

    private void ReplaceLocalVertex(Vertex vertex)
    {
        _vertices.Remove(_localVertex);
        _localVertex = vertex;
        _vertices.Add(_localVertex);
    }

    private void CleanupGraph()
    {
        var nonExistentAddresses = new HashSet<string>(_vertices.Count + 1);

        foreach (var vertex in _vertices)
        {
            nonExistentAddresses.Add(vertex.Neighborhood.Address);
        }

        foreach (var vertex in _vertices)
        {
            foreach (var address in vertex.Neighborhood.Neighbors)
            {
                nonExistentAddresses.Remove(address);
            }
        }

        _vertices.RemoveAll(
            item => item.Neighborhood.Address != _localVertex.Neighborhood.Address
            && nonExistentAddresses.Contains(item.Neighborhood.Address));
    }

    private int LocalAdjacencyChanged(Vertex vertex2)
    => (_localVertex.Neighborhood.Neighbors.Contains(vertex2.Neighborhood.Address) ? 1 : 0)
       - (vertex2.Neighborhood.Neighbors.Contains(_localVertex.Neighborhood.Address) ? 1 : 0);
}
