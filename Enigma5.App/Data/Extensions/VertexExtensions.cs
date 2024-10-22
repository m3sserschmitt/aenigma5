/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using Enigma5.App.Common.Constants;
using Enigma5.App.Models;

namespace Enigma5.App.Data.Extensions;

public static class VertexExtensions
{
    public static VertexBroadcastRequest ToVertexBroadcast(this Vertex vertex)
    => new(vertex.PublicKey ?? string.Empty, vertex.SignedData ?? string.Empty);

    public static bool IsExpired(this Vertex? vertex, TimeSpan lifetime)
    => vertex is not null && DateTimeOffset.Now - vertex.LastUpdate > lifetime;

    public static bool IsLeafExpired(this Vertex? vertex, TimeSpan lifetime)
    => vertex is not null && vertex.IsLeaf && vertex.IsExpired(lifetime);

    public static bool IsRemovalCandidate(this Vertex? vertex, TimeSpan leafLifetime)
    => (vertex is not null && !vertex.IsLeaf) || vertex.IsLeafExpired(leafLifetime);

    public static bool ShallBeBroadcasted(this Vertex? vertex) => IsExpired(vertex, DataPersistencePeriod.VertexBroadcastMinimumPeriod);
}
