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

public class GetNeighborAddressesHandlerTests
{
    [Fact]
    public async Task The_neighbors_of_the_node_are_returned()
    {
        await using var node = await TestNode.StartAsync();
        await node.SignIn("connection-2", TestKeys.PrivateKey2);

        var before = await node.Send(new GetNeighborAddressesQuery());
        await node.Send(new UpdateLocalAdjacencyCommand([TestKeys.Address2], true));
        var after = await node.Send(new GetNeighborAddressesQuery());

        Assert.Empty(before.Value!);
        Assert.Equal([TestKeys.Address2], after.Value!);
    }
}
