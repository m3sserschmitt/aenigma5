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

public class GetPeersHandlerTests
{
    [Fact]
    public async Task The_stored_peers_are_listed()
    {
        await using var node = await TestNode.StartAsync();
        await node.Send(new AddPeerCommand("http://two.example", TestKeys.Address2));
        await node.Send(new AddPeerCommand("http://three.example", TestKeys.Address3));

        var result = await node.Send(new GetPeersQuery());

        Assert.True(result.Success);
        Assert.Equal(
            [("http://three.example/", TestKeys.Address3), ("http://two.example/", TestKeys.Address2)],
            result.Value!.Select(peer => (peer.Host!, peer.Address!)).Order());
        Assert.All(result.Value!, peer => Assert.True(peer.Id > 0));
    }

    [Fact]
    public async Task Without_peers_the_list_is_empty()
    {
        await using var node = await TestNode.StartAsync();

        Assert.Empty((await node.Send(new GetPeersQuery())).Value!);
    }
}
