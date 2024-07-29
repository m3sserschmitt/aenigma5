using Enigma5.App.Models;

namespace Enigma5.App.Data.Extensions;

public static class VertexExtensions
{
    public static VertexBroadcastRequest ToVertexBroadcast(this Vertex vertex)
    => new(vertex.PublicKey ?? string.Empty, vertex.SignedData ?? string.Empty);
}
