using System.Text;
using System.Text.Json;
using Enigma5.App.Security;
using Enigma5.Crypto;

namespace Enigma5.App.Data;

public class Vertex
{
    public Vertex()
    {
        PublicKey = string.Empty;
        SignedData = string.Empty;
        Neighborhood = new();
    }

    public Vertex(Neighborhood neighborhood, string publicKey, string signature)
    {
        PublicKey = publicKey;
        SignedData = signature;
        Neighborhood = neighborhood;
    }

    public string PublicKey { get; set; }

    public string SignedData { get; set; }

    public Neighborhood Neighborhood { get; set; }

    public static Vertex Create(
        string publicKey,
        string privateKey,
        string address,
        List<string> neighbors,
        string? passphrase = null,
        string? hostname = null)
    {
        var neighborhood = new Neighborhood(neighbors, address, hostname);
        var serializedNeighborhood = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(neighborhood));
        using var envelope = Envelope.Factory.CreateSignature(privateKey, passphrase ?? string.Empty);
        var signature = envelope.Sign(serializedNeighborhood);

        return new Vertex(neighborhood, publicKey, Convert.ToBase64String(signature!));
    }

    public static Vertex Create(CertificateManager certificateManager, List<string> neighbors, string? hostname = null)
    => Create(
        certificateManager.PublicKey,
        certificateManager.PrivateKey,
        certificateManager.Address,
        neighbors,
        null,
        hostname);

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

        return obj1.Neighborhood == obj2.Neighborhood;
    }

    public static bool operator !=(Vertex? obj1, Vertex? obj2)
    {
        return !(obj1 == obj2);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Vertex other)
        {
            return this == other;
        }
        return false;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
