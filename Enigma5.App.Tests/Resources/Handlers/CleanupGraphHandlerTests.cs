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

public class CleanupGraphHandlerTests
{
    [Fact]
    public async Task The_number_of_removed_vertices_is_returned()
    {
        await using var node = await TestNode.StartAsync(("UnlistedVertexGracePeriod", "00:00:00"));
        var other = await Vertex.Factory.CreateAsync(FixedKeyCertificateManager.Key2(), []);
        await node.Send(new HandleBroadcastCommand(other.ToVertexBroadcast()));
        await Task.Delay(20);

        var first = await node.Send(new CleanupGraphCommand());
        var second = await node.Send(new CleanupGraphCommand());

        Assert.Equal((true, 1), (first.Success, first.Value));
        Assert.Equal((true, 0), (second.Success, second.Value));
        Assert.Null(await node.Get<NetworkGraph>().GetVertexAsync(TestKeys.Address2));
    }
}
