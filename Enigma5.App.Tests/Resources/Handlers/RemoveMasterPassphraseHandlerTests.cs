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

public class RemoveMasterPassphraseHandlerTests
{
    [Fact]
    public async Task Locking_leaves_the_node_without_signed_vertex_and_shows_it_on_the_dashboard()
    {
        await using var node = await TestNode.StartAsync();
        await node.Get<DashboardUIState>().SetPrivateKeyUnlockedAsync(true);
        await node.SignIn("connection-2", TestKeys.PrivateKey2);
        await node.Send(new UpdateLocalAdjacencyCommand([TestKeys.Address2], true));

        var result = await node.Send(new RemoveMasterPassphraseCommand());

        Assert.True(result.Success);
        Assert.True(result.Value);
        Assert.True(node.Key.Locked);
        var local = await node.Get<NetworkGraph>().GetLocalVertexAsync();
        Assert.Null(local.SignedData);
        Assert.Empty(local.Neighborhood.Neighbors);
        Assert.False(node.Get<DashboardUIState>().PrivateKeyUnlocked);
    }
}
