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

using Enigma5.App.Common.Utils;
using Enigma5.App.Data;
using Enigma5.App.UI;
using Enigma5.Tests.Base;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Tests.Data;

// The node has key 1, whose passphrase can be taken away. Keys 2 and 3 belong to other nodes.
public sealed class NetworkGraphTests : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    private readonly FixedKeyCertificateManager _node = FixedKeyCertificateManager.Key1();

    private readonly FixedKeyCertificateManager _peer = FixedKeyCertificateManager.Key2();

    private readonly FixedKeyCertificateManager _other = FixedKeyCertificateManager.Key3();

    private DashboardUIState _dashboard = null!;

    public void Dispose() => _disposables.ForEach(item => item.Dispose());

    private async Task<NetworkGraph> Graph(params (string Key, string? Value)[] settings)
    {
        IConfiguration configuration = TestConfiguration.Create([("Hostname", "http://node.example"), .. settings]);
        var dashboardRunner = new SimpleSingleThreadRunner();
        _dashboard = new DashboardUIState(dashboardRunner, new CapturingLogger<DashboardUIState>());
        var graph = new NetworkGraph(_node, new NetworkGraphValidationPolicy(configuration), configuration,
            new SimpleSingleThreadRunner(), new CapturingLogger<NetworkGraph>(), _dashboard);
        _disposables.Add(graph);
        _disposables.Add(dashboardRunner);
        Assert.True(await graph.GenerateLocalVertexAsync());
        return graph;
    }

    private static async Task<Vertex> VertexOf(FixedKeyCertificateManager owner, params string[] neighbors)
    => (await Vertex.Factory.CreateAsync(owner, [.. neighbors], onionService: $"http://{owner.Address[..16]}.onion"))!;

    private static string[] Addresses(IEnumerable<Vertex> vertices) => [.. vertices.Select(vertex => vertex.Neighborhood.Address!).Order()];

    #region The local vertex

    [Fact]
    public async Task A_new_graph_holds_an_unsigned_local_vertex_until_one_is_generated()
    {
        IConfiguration configuration = TestConfiguration.Create();
        using var dashboardRunner = new SimpleSingleThreadRunner();
        using var graph = new NetworkGraph(_node, new NetworkGraphValidationPolicy(configuration), configuration, new SimpleSingleThreadRunner(),
            new CapturingLogger<NetworkGraph>(), new DashboardUIState(dashboardRunner, new CapturingLogger<DashboardUIState>()));

        var local = await graph.GetLocalVertexAsync();

        Assert.Null(local.SignedData);
        Assert.Null(local.Neighborhood.Address);
        Assert.Null(await graph.GetGraphHashAsync());
    }

    [Fact]
    public async Task GenerateLocalVertexAsync_signs_a_vertex_with_the_names_from_the_settings()
    {
        var graph = await Graph(("OnionService", "http://example.onion"));

        var local = await graph.GetLocalVertexAsync();

        Assert.Equal(TestKeys.Address1, local.Neighborhood.Address);
        Assert.Equal("http://node.example", local.Neighborhood.Hostname);
        Assert.Equal("http://example.onion", local.Neighborhood.OnionService);
        Assert.NotNull(local.SignedData);
        Assert.Equal([TestKeys.Address1], Addresses(await graph.GetVerticesAsync()));
    }

    [Fact]
    public async Task GenerateLocalVertexAsync_gives_an_unsigned_vertex_without_neighbors_when_the_key_cannot_sign()
    {
        var graph = await Graph();
        await graph.AddAdjacencyAsync([TestKeys.Address2]);
        _node.Locked = true;

        Assert.False(await graph.GenerateLocalVertexAsync());

        var local = await graph.GetLocalVertexAsync();
        Assert.Null(local.SignedData);
        Assert.Equal(TestKeys.Address1, local.Neighborhood.Address);
        Assert.Empty(local.Neighborhood.Neighbors);
    }

    [Fact]
    public async Task Neighbors_are_added_to_and_removed_from_the_local_vertex()
    {
        var graph = await Graph();

        var added = await graph.AddAdjacencyAsync([TestKeys.Address2, TestKeys.Address3]);
        var removed = await graph.RemoveAdjacencyAsync([TestKeys.Address2]);

        Assert.Equal([TestKeys.Address2, TestKeys.Address3], added.Neighborhood.Neighbors.Order());
        Assert.Equal([TestKeys.Address3], removed.Neighborhood.Neighbors);
        Assert.Equal([TestKeys.Address3], await graph.GetNeighborAddressesAsync());
        Assert.NotNull(removed.SignedData);
    }

    [Fact]
    public async Task A_locked_node_keeps_its_neighbor_list_when_asked_to_change_it()
    {
        var graph = await Graph();
        await graph.AddAdjacencyAsync([TestKeys.Address2]);
        _node.Locked = true;

        await graph.RemoveAdjacencyAsync([TestKeys.Address2]);

        Assert.Equal([TestKeys.Address2], await graph.GetNeighborAddressesAsync());
    }

    #endregion

    #region Vertices of other nodes

    [Fact]
    public async Task A_vertex_that_lists_the_node_makes_its_owner_a_neighbor_when_the_owner_has_a_session()
    {
        var graph = await Graph();

        var updated = await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);

        // Both vertices changed: the peer's is new, and the node's own now lists the peer.
        Assert.Equal([TestKeys.Address2, TestKeys.Address1], Addresses(updated));
        Assert.Equal([TestKeys.Address2], await graph.GetNeighborAddressesAsync());
        Assert.Equal([TestKeys.Address2, TestKeys.Address1], Addresses(await graph.GetVerticesAsync()));
    }

    [Fact]
    public async Task Without_a_session_the_vertex_is_stored_but_its_owner_does_not_become_a_neighbor()
    {
        var graph = await Graph();

        var updated = await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: false);

        Assert.Equal([TestKeys.Address2], Addresses(updated));
        Assert.Empty(await graph.GetNeighborAddressesAsync());
        Assert.NotNull(await graph.GetVertexAsync(TestKeys.Address2));
    }

    [Fact]
    public async Task A_vertex_that_no_longer_lists_the_node_ends_the_neighborhood_even_without_a_session()
    {
        var graph = await Graph();
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);

        await graph.UpdateAsync(await VertexOf(_peer), mayAddNeighbor: false);

        Assert.Empty(await graph.GetNeighborAddressesAsync());
    }

    [Fact]
    public async Task A_vertex_that_is_not_valid_is_dropped()
    {
        var graph = await Graph();
        var signed = await VertexOf(_peer, TestKeys.Address1);
        var changed = new Vertex(new([TestKeys.Address1, TestKeys.Address3], signed.Neighborhood.Address, null, signed.Neighborhood.OnionService, signed.Neighborhood.LastUpdate), signed.PublicKey, signed.SignedData);

        Assert.Empty(await graph.UpdateAsync(changed, mayAddNeighbor: true));

        Assert.Null(await graph.GetVertexAsync(TestKeys.Address2));
        Assert.Empty(await graph.GetNeighborAddressesAsync());
    }

    [Fact]
    public async Task The_node_ignores_its_own_vertex_when_it_comes_back_from_the_network()
    {
        var graph = await Graph();
        var own = await VertexOf(_node, TestKeys.Address3);

        Assert.Empty(await graph.UpdateAsync(own, mayAddNeighbor: true));

        Assert.Empty(await graph.GetNeighborAddressesAsync());
    }

    [Fact]
    public async Task A_newer_vertex_with_other_content_replaces_the_stored_one_and_the_same_content_does_not()
    {
        var graph = await Graph();
        var first = await VertexOf(_other, TestKeys.Address2);
        await graph.UpdateAsync(first, mayAddNeighbor: false);
        await Task.Delay(20);

        var sameContent = await graph.UpdateAsync(await VertexOf(_other, TestKeys.Address2), mayAddNeighbor: false);
        var otherContent = await graph.UpdateAsync(await VertexOf(_other), mayAddNeighbor: false);

        Assert.Empty(sameContent);
        Assert.Equal([TestKeys.Address3], Addresses(otherContent));
        Assert.Empty((await graph.GetVertexAsync(TestKeys.Address3))!.Neighborhood.Neighbors);
    }

    [Fact]
    public async Task Vertices_are_handed_out_as_copies()
    {
        var graph = await Graph();
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);

        (await graph.GetVertexAsync(TestKeys.Address2))!.Neighborhood.Neighbors.Clear();
        (await graph.GetLocalVertexAsync()).Neighborhood.Neighbors.Clear();

        Assert.Equal([TestKeys.Address1], (await graph.GetVertexAsync(TestKeys.Address2))!.Neighborhood.Neighbors);
        Assert.Equal([TestKeys.Address2], await graph.GetNeighborAddressesAsync());
        Assert.Null(await graph.GetVertexAsync(TestKeys.Address3));
    }

    [Fact]
    public async Task The_graph_hash_changes_with_the_content_and_not_with_the_time()
    {
        var graph = await Graph();
        var alone = await graph.GetGraphHashAsync();

        await graph.UpdateAsync(await VertexOf(_other, TestKeys.Address2), mayAddNeighbor: false);
        var withOther = await graph.GetGraphHashAsync();
        await Task.Delay(20);
        await graph.GenerateLocalVertexAsync();

        Assert.NotNull(alone);
        Assert.NotEqual(alone, withOther);
        Assert.Equal(withOther, await graph.GetGraphHashAsync());
    }

    #endregion

    #region Cleanup

    [Fact]
    public async Task Cleanup_removes_a_vertex_older_than_the_lifetime_even_when_it_is_listed()
    {
        var graph = await Graph(("VertexLifetime", "00:00:01"));
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);

        Assert.Equal(0, await graph.CleanupAsync());
        await Task.Delay(1200);
        var removed = await graph.CleanupAsync();

        Assert.Equal(1, removed);
        Assert.Equal([TestKeys.Address1], Addresses(await graph.GetVerticesAsync()));
    }

    [Fact]
    public async Task Cleanup_removes_a_vertex_that_nobody_lists_only_after_the_grace_period()
    {
        var graph = await Graph(("UnlistedVertexGracePeriod", "00:00:01"));
        await graph.UpdateAsync(await VertexOf(_other), mayAddNeighbor: false);

        Assert.Equal(0, await graph.CleanupAsync());
        await Task.Delay(1200);

        Assert.Equal(1, await graph.CleanupAsync());
        Assert.Null(await graph.GetVertexAsync(TestKeys.Address3));
    }

    [Fact]
    public async Task Cleanup_keeps_a_vertex_that_another_vertex_lists()
    {
        var graph = await Graph(("UnlistedVertexGracePeriod", "00:00:01"));
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);
        await graph.UpdateAsync(await VertexOf(_other, TestKeys.Address2), mayAddNeighbor: false);
        await Task.Delay(1200);

        var removed = await graph.CleanupAsync();

        // The peer is listed by the node; the third vertex is listed by nobody.
        Assert.Equal(1, removed);
        Assert.Equal([TestKeys.Address2, TestKeys.Address1], Addresses(await graph.GetVerticesAsync()));
    }

    [Fact]
    public async Task Cleanup_never_removes_the_local_vertex()
    {
        var graph = await Graph(("VertexLifetime", "00:00:00"), ("UnlistedVertexGracePeriod", "00:00:00"));
        await Task.Delay(20);

        Assert.Equal(0, await graph.CleanupAsync());
        Assert.Equal([TestKeys.Address1], Addresses(await graph.GetVerticesAsync()));
    }

    #endregion

    #region The list on the dashboard

    [Fact]
    public async Task A_peer_is_shown_when_both_nodes_list_each_other()
    {
        var graph = await Graph();

        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);

        var shown = Assert.Single(_dashboard.InboundPeers);
        Assert.Equal(TestKeys.Address2, shown.Address);
        Assert.True(shown.Connected);
        Assert.Null(shown.Id);
    }

    [Fact]
    public async Task The_host_of_a_shown_peer_is_its_hostname_or_else_its_onion_service()
    {
        var graph = await Graph();
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);

        Assert.Equal($"http://{TestKeys.Address2[..16]}.onion", Assert.Single(_dashboard.InboundPeers).Host);

        var second = await Graph();
        var withHostname = (await Vertex.Factory.CreateAsync(_peer, [TestKeys.Address1], "http://peer.example", "http://ignored.onion"))!;
        await second.UpdateAsync(withHostname, mayAddNeighbor: true);

        Assert.Equal("http://peer.example", Assert.Single(_dashboard.InboundPeers).Host);
    }

    [Fact]
    public async Task A_peer_that_lists_the_node_is_not_shown_while_the_node_does_not_list_it()
    {
        var graph = await Graph();

        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: false);

        Assert.Empty(_dashboard.InboundPeers);
    }

    // The last vertex of the peer still lists the node; only the node's own list has changed.
    [Fact]
    public async Task A_peer_is_no_longer_shown_as_soon_as_the_node_drops_it()
    {
        var graph = await Graph();
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);

        await graph.RemoveAdjacencyAsync([TestKeys.Address2]);

        Assert.Empty(_dashboard.InboundPeers);
        Assert.NotNull(await graph.GetVertexAsync(TestKeys.Address2));
    }

    [Fact]
    public async Task No_peer_is_shown_after_the_node_is_locked()
    {
        var graph = await Graph();
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);
        _node.Locked = true;

        await graph.GenerateLocalVertexAsync();

        Assert.Empty(_dashboard.InboundPeers);
    }

    // After a lock and unlock, the peer sends a vertex with the content the node already has. The node lists
    // the peer again, and the dashboard must follow although the vertex itself is not new.
    [Fact]
    public async Task A_peer_is_shown_again_when_it_comes_back_with_a_vertex_the_node_already_has()
    {
        var graph = await Graph();
        var vertex = await VertexOf(_peer, TestKeys.Address1);
        await graph.UpdateAsync(vertex, mayAddNeighbor: true);
        await graph.RemoveAdjacencyAsync([TestKeys.Address2]);

        await graph.UpdateAsync(vertex, mayAddNeighbor: true);

        Assert.Equal([TestKeys.Address2], await graph.GetNeighborAddressesAsync());
        Assert.Equal(TestKeys.Address2, Assert.Single(_dashboard.InboundPeers).Address);
    }

    [Fact]
    public async Task A_peer_is_no_longer_shown_after_cleanup_removed_its_vertex()
    {
        var graph = await Graph(("VertexLifetime", "00:00:01"));
        await graph.UpdateAsync(await VertexOf(_peer, TestKeys.Address1), mayAddNeighbor: true);
        await Task.Delay(1200);

        await graph.CleanupAsync();

        Assert.Empty(_dashboard.InboundPeers);
    }

    #endregion
}
