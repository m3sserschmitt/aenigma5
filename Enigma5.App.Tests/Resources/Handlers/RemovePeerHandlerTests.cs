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

public class RemovePeerHandlerTests
{
    [Fact]
    public async Task A_stored_peer_is_removed_from_the_database_and_the_dashboard()
    {
        await using var node = await TestNode.StartAsync();
        var kept = (await node.Send(new AddPeerCommand("http://kept.example", TestKeys.Address3))).Value!;
        var removed = (await node.Send(new AddPeerCommand("http://peer.example", TestKeys.Address2))).Value!;

        var result = await node.Send(new RemovePeerCommand(removed.Id!.Value));

        Assert.True(result.Success);
        Assert.Equal(1, result.Value);
        Assert.Equal([kept.Id!.Value], await node.Database(context => context.Peers.Select(peer => peer.Id).ToListAsync()));
        Assert.Equal([TestKeys.Address3], node.Get<DashboardUIState>().OutboundPeers.Select(peer => peer.Address));
    }

    [Fact]
    public async Task An_unknown_id_succeeds_and_reports_that_no_peer_was_removed()
    {
        await using var node = await TestNode.StartAsync();
        Assert.True((await node.Send(new AddPeerCommand("http://kept.example", TestKeys.Address3))).Success);

        var result = await node.Send(new RemovePeerCommand(12345));

        Assert.Equal((true, 0), (result.Success, result.Value));
        Assert.Equal(1, await node.Database(context => context.Peers.CountAsync()));
    }
}
