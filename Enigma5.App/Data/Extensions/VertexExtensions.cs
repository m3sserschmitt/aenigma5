/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.App.Models;

namespace Enigma5.App.Data.Extensions;

public static class VertexExtensions
{
    public static VertexBroadcastRequestDto ToVertexBroadcast(this Vertex? vertex)
    => new(vertex?.PublicKey ?? string.Empty, vertex?.SignedData ?? string.Empty);

    // A vertex without a time has no age; it counts as expired, so that it is refused and removed.
    public static bool LastUpdateExceeded(this Vertex? vertex, TimeSpan lifetime)
    => vertex is not null
    && (vertex.Neighborhood.LastUpdate is null || DateTimeOffset.UtcNow - vertex.Neighborhood.LastUpdate > lifetime);

    // The clocks of nodes differ a little; a time further ahead would keep the vertex for longer than its lifetime.
    public static bool LastUpdateInFuture(this Vertex vertex)
    => vertex.Neighborhood.LastUpdate > DateTimeOffset.UtcNow + Common.Constants.MaxVertexClockSkew;

    public static bool ShouldReplace(this Vertex vertex, Vertex previous)
    {
        var timeInterval = vertex.Neighborhood.LastUpdate - previous.Neighborhood.LastUpdate;
        var timeIntervalOk = timeInterval.HasValue && timeInterval.Value.Ticks > 0;
        if(!timeIntervalOk)
        {
            return false;
        }
        var sameNeighborhood = vertex.Neighborhood == previous.Neighborhood;
        return !sameNeighborhood || (sameNeighborhood && timeInterval > Common.Constants.DefaultVertexBroadcastMinimumPeriod);
    }
}
