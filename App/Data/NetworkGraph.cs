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

    public Vertex Add(string address)
    {
        _mutex.WaitOne();

        if (!_localVertex.Neighborhood.Neighbors.Contains(address))
        {
            _vertices.Remove(_localVertex);
            _localVertex = Vertex.Factory.Prototype.AddNeighbor(_localVertex, address, _certificateManager);
            _vertices.Add(_localVertex);
        }

        _mutex.ReleaseMutex();


        return LocalVertex;
    }

    public Task<Vertex> AddAsync(string address, CancellationToken cancellationToken = default)
    {
        Vertex task() => Add(address);
        return Task.Run(task, cancellationToken);
    }

    public (IList<Vertex> vertices, Delta delta) Add(Vertex vertex)
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
        }
        else
        {
            ReplaceLocalVertex(vertex);
            vertices.Add(_localVertex);
        }

        CleanupGraph();

        _mutex.ReleaseMutex();

        return (vertices, delta);
    }

    public Task<(IList<Vertex> vertices, Delta delta)> AddAsync(Vertex vertex, CancellationToken cancellationToken = default)
    {
        (IList<Vertex>, Delta delta) task() => Add(vertex);
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
            newLocalVertex = Vertex.Factory.Prototype.AddNeighbor(_localVertex, source, _certificateManager);
            added = true;
        }
        else if (result > 0)
        {
            newLocalVertex = Vertex.Factory.Prototype.RemoveNeighbor(_localVertex, source, _certificateManager);
        }

        if (result == 0)
        {
            return (false, new());
        }

        ReplaceLocalVertex(newLocalVertex!);

        return (true, new Delta { Vertex = source, Added = added });
    }

    private void ReplaceLocalVertex(Vertex vertex)
    {
        _vertices.Remove(_localVertex);
        _localVertex = vertex;
        _vertices.Add(_localVertex);
    }

    private void CleanupGraph()
    {

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
