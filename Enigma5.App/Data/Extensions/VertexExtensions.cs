using Enigma5.App.Models;

namespace Enigma5.App.Data.Extensions;

public static class VertexExtensions
{
    public static VertexBroadcastRequest ToVertexBroadcast(this Vertex vertex)
    => new(vertex.PublicKey ?? string.Empty, vertex.SignedData ?? string.Empty);

    public static bool IsRemovalCandidate(this Vertex vertex, TimeSpan timeSpan)
    => !vertex.IsLeaf || (vertex.IsLeaf && DateTimeOffset.Now - vertex.LastUpdate > timeSpan);
}
