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
using Enigma5.App.Models;
using Enigma5.App.NetworkBridge;
using Enigma5.Tests.Base;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.NetworkBridge;

// Nothing is connected in these tests: a vector only opens its connections when it is started.
public class HubConnectionsProxyTests
{
    private static readonly (string, string?)[] Endpoint = [("Kestrel:EndPoints:Http:Url", "http://127.0.0.1:8080")];

    private static async Task<TestNode> NodeWithPeer()
    {
        var node = await TestNode.StartAsync(Endpoint);
        await node.Database(async context =>
        {
            context.Peers.Add(new Peer { Host = "http://peer.example/", Address = TestKeys.Address2 });
            await context.SaveChangesAsync();
        });
        return node;
    }

    private static ConnectionVector VectorFor(TestNode node, string host)
    => Assert.Single(ConnectionVector.CreateConnections("http://127.0.0.1:8080", [new PeerDto { Host = host, Address = TestKeys.Address2 }],
        node.Get<NetworkGraphValidationPolicy>(), node.Key, node.Configuration, new CapturingLogger<HubConnectionsProxyTests>()));

    [Fact]
    public async Task Two_vectors_for_the_same_hosts_are_equal_but_not_the_same_object()
    {
        await using var node = await NodeWithPeer();

        var first = VectorFor(node, "http://peer.example/");
        var second = VectorFor(node, "http://peer.example/");

        Assert.Equal(first, second);
        Assert.NotSame(first, second);
        Assert.NotEqual(first, VectorFor(node, "http://other.example/"));
    }

    // A removal asked for a closed vector must not take the vector that has replaced it.
    [Fact]
    public async Task A_removal_takes_only_the_very_vector_it_was_asked_for()
    {
        await using var node = await NodeWithPeer();
        var proxy = node.Get<HubConnectionsProxy>();
        Assert.True(await proxy.LoadConnectionsAsync());
        var equalButOther = VectorFor(node, "http://peer.example/");

        Assert.False(proxy.RemoveConnection(equalButOther));
    }

    [Fact]
    public async Task A_vector_for_a_peer_that_is_not_loaded_cannot_be_removed()
    {
        await using var node = await NodeWithPeer();
        var proxy = node.Get<HubConnectionsProxy>();
        await proxy.LoadConnectionsAsync();

        Assert.False(proxy.RemoveConnection(VectorFor(node, "http://other.example/")));
    }

    [Fact]
    public async Task Loading_fails_without_a_listen_address_and_succeeds_without_peers()
    {
        await using var withoutEndpoint = await TestNode.StartAsync();
        await withoutEndpoint.Database(async context =>
        {
            context.Peers.Add(new Peer { Host = "http://peer.example/", Address = TestKeys.Address2 });
            await context.SaveChangesAsync();
        });
        await using var withoutPeers = await TestNode.StartAsync(Endpoint);

        Assert.False(await withoutEndpoint.Get<HubConnectionsProxy>().LoadConnectionsAsync());
        Assert.True(await withoutPeers.Get<HubConnectionsProxy>().LoadConnectionsAsync());
        Assert.Equal(0, await withoutPeers.Database(context => context.Peers.CountAsync()));
    }
}
