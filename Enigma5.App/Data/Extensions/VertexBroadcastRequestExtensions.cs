using Enigma5.App.Models;

namespace Enigma5.App.Data.Extensions;

public static class VertexBroadcastRequestExtensions
{
    public static Vertex ToVertex(this VertexBroadcastRequest vertexBroadcast)
    => new(vertexBroadcast.AdjacencyList.ToNeighborhood(), vertexBroadcast.PublicKey, vertexBroadcast.SignedData);
}
