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

using Enigma5.App.Data;
using Enigma5.Tests.Base;

namespace Enigma5.App.Tests.Data;

public class NeighborhoodTests
{
    private static Neighborhood Create(string[] neighbors, string? hostname = "http://node.example", string? onion = null, DateTimeOffset? lastUpdate = null)
    => new([.. neighbors], TestKeys.Address1, hostname, onion, lastUpdate ?? DateTimeOffset.UtcNow);

    [Fact]
    public void Two_neighborhoods_are_equal_whatever_the_order_of_neighbors_and_the_time()
    {
        var first = Create([TestKeys.Address2, TestKeys.Address3], lastUpdate: DateTimeOffset.UtcNow);
        var second = Create([TestKeys.Address3, TestKeys.Address2], lastUpdate: DateTimeOffset.UtcNow.AddHours(-1));

        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_different_neighbor_hostname_or_onion_service_makes_them_differ()
    {
        var neighborhood = Create([TestKeys.Address2]);

        Assert.True(neighborhood != Create([TestKeys.Address3]));
        Assert.True(neighborhood != Create([TestKeys.Address2], hostname: "http://other.example"));
        Assert.True(neighborhood != Create([TestKeys.Address2], onion: "http://example.onion"));
        Assert.False(neighborhood == null);
    }

    [Fact]
    public void The_neighbors_are_copied_from_the_given_set()
    {
        var neighbors = new HashSet<string> { TestKeys.Address2 };
        var neighborhood = new Neighborhood(neighbors, TestKeys.Address1, null, null, null);

        neighbors.Add(TestKeys.Address3);

        Assert.Equal([TestKeys.Address2], neighborhood.Neighbors);
    }
}
