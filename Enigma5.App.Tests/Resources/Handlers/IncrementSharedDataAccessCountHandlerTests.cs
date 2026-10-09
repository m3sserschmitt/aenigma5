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
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Queries;
using Enigma5.Tests.Base;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Resources.Handlers;

public class IncrementSharedDataAccessCountHandlerTests
{
    [Fact]
    public async Task Each_call_counts_one_access_and_the_data_is_removed_at_its_maximum()
    {
        await using var node = await TestNode.StartAsync();
        var created = await node.Send(new CreateSharedDataCommand(new SharedDataCreateDto(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3, 2)));
        var tag = created.Value!.Tag!;

        var first = await node.Send(new IncrementSharedDataAccessCountCommand(tag));
        var stillThere = (await node.Send(new GetSharedDataQuery(tag))).Value is not null;
        var second = await node.Send(new IncrementSharedDataAccessCountCommand(tag));
        var third = await node.Send(new IncrementSharedDataAccessCountCommand(tag));

        Assert.Equal((true, 1), (first.Success, first.Value));
        Assert.True(stillThere);
        Assert.Equal((true, 1), (second.Success, second.Value));
        // The command succeeds and reports that no record was changed; the endpoint answers 404 for it.
        Assert.Equal((true, 0), (third.Success, third.Value));
        Assert.Null((await node.Send(new GetSharedDataQuery(tag))).Value);
    }

    [Fact]
    public async Task An_unknown_tag_changes_no_record()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new IncrementSharedDataAccessCountCommand(Guid.NewGuid().ToString()));

        Assert.Equal((true, 0), (result.Success, result.Value));
    }
}
