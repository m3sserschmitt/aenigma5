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

using Enigma5.App.Common;
using Enigma5.App.Data;
using Enigma5.App.Data.Extensions;
using Enigma5.Tests.Base;

namespace Enigma5.App.Tests.Data.Extensions;

public class VertexExtensionsTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static Vertex Create(DateTimeOffset? lastUpdate, params string[] neighbors)
    => new(new([.. neighbors], TestKeys.Address1, null, null, lastUpdate), null, null);

    #region LastUpdateExceeded

    [Fact]
    public void LastUpdateExceeded_is_true_for_a_vertex_older_than_the_lifetime()
    {
        Assert.True(Create(Now.AddMinutes(-31)).LastUpdateExceeded(TimeSpan.FromMinutes(30)));
        Assert.False(Create(Now.AddMinutes(-29)).LastUpdateExceeded(TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void LastUpdateExceeded_is_false_without_a_vertex_or_a_time()
    {
        Assert.False(((Vertex?)null).LastUpdateExceeded(TimeSpan.Zero));
        Assert.False(Create(null).LastUpdateExceeded(TimeSpan.Zero));
    }

    #endregion

    #region ShouldReplace

    [Fact]
    public void A_newer_vertex_with_other_neighbors_replaces_the_stored_one()
    {
        var stored = Create(Now.AddSeconds(-10), TestKeys.Address2);

        Assert.True(Create(Now, TestKeys.Address3).ShouldReplace(stored));
        Assert.True(Create(Now).ShouldReplace(stored));
    }

    [Fact]
    public void A_newer_vertex_with_the_same_content_replaces_the_stored_one_only_after_the_minimum_period()
    {
        var withinThePeriod = Create(Now - Constants.DefaultVertexBroadcastMinimumPeriod + TimeSpan.FromSeconds(5), TestKeys.Address2);
        var beyondThePeriod = Create(Now - Constants.DefaultVertexBroadcastMinimumPeriod - TimeSpan.FromSeconds(5), TestKeys.Address2);

        Assert.False(Create(Now, TestKeys.Address2).ShouldReplace(withinThePeriod));
        Assert.True(Create(Now, TestKeys.Address2).ShouldReplace(beyondThePeriod));
    }

    [Fact]
    public void A_vertex_that_is_not_newer_never_replaces_the_stored_one()
    {
        var stored = Create(Now, TestKeys.Address2);

        Assert.False(Create(Now, TestKeys.Address3).ShouldReplace(stored));
        Assert.False(Create(Now.AddSeconds(-1), TestKeys.Address3).ShouldReplace(stored));
    }

    [Fact]
    public void A_vertex_without_a_time_never_replaces_and_is_never_replaced()
    {
        Assert.False(Create(null, TestKeys.Address3).ShouldReplace(Create(Now, TestKeys.Address2)));
        Assert.False(Create(Now, TestKeys.Address3).ShouldReplace(Create(null, TestKeys.Address2)));
    }

    #endregion

    #region Conversions

    [Fact]
    public async Task A_vertex_survives_the_way_through_a_broadcast_request()
    {
        var policy = new NetworkGraphValidationPolicy(TestConfiguration.Create());
        var vertex = await Vertex.Factory.CreateAsync(FixedKeyCertificateManager.Key1(), [TestKeys.Address2], "http://node.example", "http://example.onion");

        var received = vertex.ToVertexBroadcast().ToVertex();

        Assert.Equal(vertex!.PublicKey, received.PublicKey);
        Assert.Equal(vertex.SignedData, received.SignedData);
        Assert.True(vertex.Neighborhood == received.Neighborhood);
        Assert.Equal(vertex.Neighborhood.LastUpdate, received.Neighborhood.LastUpdate);
        Assert.True(policy.Validate(received));
    }

    [Fact]
    public void ToVertexBroadcast_of_no_vertex_gives_an_empty_request()
    {
        var request = ((Vertex?)null).ToVertexBroadcast();

        Assert.Equal(string.Empty, request.PublicKey);
        Assert.Null(request.SignedData);
    }

    #endregion
}
