using System.Text.Json;
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

            var serializedData = JsonSerializer.Serialize(_vertices);
            var vertices = JsonSerializer.Deserialize<List<Vertex>>(serializedData);

            _mutex.ReleaseMutex();

            return vertices!;
        }
    }

    public NetworkGraph(CertificateManager certificateManager, IConfiguration configuration)
    {
        _mutex = new();
        _certificateManager = certificateManager;
        _configuration = configuration;
        _localVertex = Vertex.Create(
            _certificateManager,
            new List<string>(),
            _configuration.GetValue<string>("Hostname")
            );
        _vertices = new() { _localVertex };
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
            _vertices.Remove(_localVertex);
            _localVertex = vertex;
            _vertices.Add(_localVertex);
            vertices.Add(_localVertex);
        }

        _mutex.ReleaseMutex();

        return (vertices, delta);
    }

    public void Revert(Delta delta)
    {
        if (delta.Vertex == null)
        {
            return;
        }

        _mutex.WaitOne();

        if (delta.Added)
        {
            _vertices.Remove(_localVertex);
            var newNeighborhood = _localVertex.Neighborhood.Neighbors.Where(item => item != delta.Vertex.Neighborhood.Address);
            _localVertex = Vertex.Create(_certificateManager, newNeighborhood.ToList(), _localVertex.Neighborhood.Hostname);
            _vertices.Add(_localVertex);
        }
        else
        {
            _vertices.Remove(_localVertex);
            var newNeighborhood = new List<string>(_localVertex.Neighborhood.Neighbors)
            {
                delta.Vertex.Neighborhood.Address
            };
            _localVertex = Vertex.Create(_certificateManager, newNeighborhood, _localVertex.Neighborhood.Hostname);
            _vertices.Add(_localVertex);
        }

        _mutex.ReleaseMutex();
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

        if (result < 0)
        {
            _localVertex.Neighborhood.Neighbors.Add(source.Neighborhood.Address);
            added = true;
        }
        else if (result > 0)
        {
            _localVertex.Neighborhood.Neighbors.Remove(source.Neighborhood.Address);
            CleanupGraph(source.Neighborhood.Address);
        }

        if (result == 0)
        {
            return (false, new());
        }

        _vertices.Remove(_localVertex);
        _localVertex = Vertex.Create(
            _certificateManager,
            _localVertex.Neighborhood.Neighbors,
            _localVertex.Neighborhood.Hostname);
        _vertices.Add(_localVertex);

        return (true, new Delta { Vertex = source, Added = added });
    }

    private void CleanupGraph(string address)
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
