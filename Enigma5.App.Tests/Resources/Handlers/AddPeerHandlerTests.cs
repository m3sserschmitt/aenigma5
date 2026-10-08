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
using Enigma5.App.Data.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Queries;
using Enigma5.App.UI;
using Enigma5.Tests.Base;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Resources.Handlers;

public class AddPeerHandlerTests
{
    private const string Host = "http://peer.example";

    [Fact]
    public async Task A_peer_is_stored_and_shown_on_the_dashboard()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new AddPeerCommand(Host, TestKeys.Address2));

        Assert.True(result.Success);
        Assert.Equal(TestKeys.Address2, result.Value!.Address);
        Assert.Equal("http://peer.example/", result.Value.Host);
        Assert.True(result.Value.Id > 0);
        var stored = await node.Database(context => context.Peers.SingleAsync());
        Assert.Equal(("http://peer.example/", TestKeys.Address2), (stored.Host, stored.Address));
        var shown = Assert.Single(node.Get<DashboardUIState>().OutboundPeers);
        Assert.Equal((stored.Id, TestKeys.Address2), (shown.Id!.Value, shown.Address));
    }

    [Fact]
    public async Task An_address_can_be_stored_only_once()
    {
        await using var node = await TestNode.StartAsync();
        await node.Send(new AddPeerCommand(Host, TestKeys.Address2));

        var result = await node.Send(new AddPeerCommand("http://other.example", TestKeys.Address2));

        Assert.False(result.Success);
        Assert.Equal(1, await node.Database(context => context.Peers.CountAsync()));
    }

    [Theory]
    [InlineData("peer.example", "a186a6fba0ff7570b116b3df639e3713fab0a21f1cf62fb616d84c19217c8023")]
    [InlineData("", "a186a6fba0ff7570b116b3df639e3713fab0a21f1cf62fb616d84c19217c8023")]
    [InlineData(Host, "not-an-address")]
    [InlineData(Host, "A186A6FBA0FF7570B116B3DF639E3713FAB0A21F1CF62FB616D84C19217C8023")]
    public async Task A_host_that_is_not_an_absolute_url_or_a_bad_address_is_refused(string host, string address)
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new AddPeerCommand(host, address));

        Assert.False(result.Success);
        Assert.Equal(0, await node.Database(context => context.Peers.CountAsync()));
    }
}
