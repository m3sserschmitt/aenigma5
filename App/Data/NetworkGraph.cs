using Enigma5.App.Common.Extensions;
using Enigma5.App.Security;
using Enigma5.Crypto;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Data;

public class NetworkGraph
{
    private readonly Mutex _mutex;

    private readonly CertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private Vertex _localVertex;

    private readonly List<Vertex> _vertices;

    public List<Vertex> Vertices
    {
        get
        {
            _mutex.WaitOne();

            var vertices = _vertices.CopyBySerialization();

            _mutex.ReleaseMutex();

            return vertices!;
        }
    }

    public Vertex LocalVertex
    {
        get
        {
            _mutex.WaitOne();

            var vertex = _localVertex.CopyBySerialization();

            _mutex.ReleaseMutex();

            return vertex!;
        }
    }

    public NetworkGraph(CertificateManager certificateManager, IConfiguration configuration)
    {
        _mutex = new();
        _certificateManager = certificateManager;
        _configuration = configuration;
        _localVertex = Vertex.Factory.CreateWithEmptyNeighborhood(_certificateManager, _configuration.GetHostname());
        _vertices = new() { _localVertex };
    }

    public (Vertex localVertex, bool updated) AddAdjacency(string address)
    {
        _mutex.WaitOne();

        if (Vertex.Factory.Prototype.AddNeighbor(_localVertex, address, _certificateManager, out Vertex? newVertex))
        {
            ReplaceLocalVertex(newVertex!);

            _mutex.ReleaseMutex();
            return (LocalVertex, true);
        }

        _mutex.ReleaseMutex();
        return (LocalVertex, false);
    }

    public (Vertex localVertex, bool updated) RemoveAdjacency(string address)
    {
        _mutex.WaitOne();

        if (Vertex.Factory.Prototype.RemoveNeighbor(_localVertex, address, _certificateManager, out Vertex? newVertex))
        {
            ReplaceLocalVertex(newVertex!);
            CleanupGraph();

            _mutex.ReleaseMutex();
            return (LocalVertex, true);
        }

        _mutex.ReleaseMutex();
        return (LocalVertex, false);
    }

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
        var vertices = new List<Vertex>();
        Delta delta = new();

        if (!Validate(vertex))
        {
            return (vertices, new());
        }

        _mutex.WaitOne();

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

            if(updated || previous != vertex)
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

        _mutex.ReleaseMutex();

        return (vertices, delta);
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

        _vertices.RemoveAll(item => nonExistentAddresses.Contains(item.Neighborhood.Address));
    }

    private int LocalAdjacencyChanged(Vertex vertex2)
    => (_localVertex.Neighborhood.Neighbors.Contains(vertex2.Neighborhood.Address) ? 1 : 0)
       - (vertex2.Neighborhood.Neighbors.Contains(_localVertex.Neighborhood.Address) ? 1 : 0);

    private static bool Validate(Vertex vertex)
    {
        if (!ValidateRequiredFields(vertex))
        {
            return false;
        }

        try
        {
            var decodedSignature = Convert.FromBase64String(vertex.SignedData!);
            using var envelope = Envelope.Factory.CreateSignatureVerification(vertex.PublicKey!);

            return envelope.Verify(decodedSignature);
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateRequiredFields(Vertex vertex)
    => vertex.PublicKey != null
    && vertex.SignedData != null
    && vertex.Neighborhood != null
    && vertex.Neighborhood.Address != null
    && vertex.Neighborhood.Neighbors != null;
}
