/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

    public static bool LastUpdateExceeded(this Vertex? vertex, TimeSpan lifetime)
    => vertex is not null && DateTimeOffset.Now - vertex.Neighborhood.LastUpdate > lifetime;

    public static bool ShouldReplace(this Vertex vertex, Vertex previous)
    {
        var timeInterval = vertex.Neighborhood.LastUpdate - previous.Neighborhood.LastUpdate;
        var timeIntervalOk = timeInterval.HasValue && timeInterval.Value.Ticks > 0;
        if(!timeIntervalOk)
        {
            return false;
        }
        var sameNeighborhood = vertex.Neighborhood == previous.Neighborhood;
        return !sameNeighborhood || (sameNeighborhood && timeInterval > Common.Constants.VertexBroadcastMinimumPeriod);
    }
}
