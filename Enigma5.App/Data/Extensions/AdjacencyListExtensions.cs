using Enigma5.App.Models;

namespace Enigma5.App.Data.Extensions;

public static class AdjacencyListExtensions
{
    public static Neighborhood ToNeighborhood(this AdjacencyList adjacencyList)
    => new([.. adjacencyList.Neighbors], adjacencyList.Address ?? string.Empty, adjacencyList.Hostname);
}
