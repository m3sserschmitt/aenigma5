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

public class GetSharedDataHandlerTests
{
    [Fact]
    public async Task Stored_data_is_returned_with_its_public_key()
    {
        await using var node = await TestNode.StartAsync();
        var created = await node.Send(new CreateSharedDataCommand(new SharedDataCreateDto(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3)));

        var result = await node.Send(new GetSharedDataQuery(created.Value!.Tag!));

        Assert.True(result.Success);
        Assert.Equal(created.Value.Tag, result.Value!.Tag);
        Assert.Equal(TestSignedData.SharedPayloadSignedWithKey3, result.Value.Data);
        Assert.Equal(TestKeys.PublicKey3, result.Value.PublicKey);
    }

    [Fact]
    public async Task Reading_does_not_change_the_access_count()
    {
        await using var node = await TestNode.StartAsync();
        var created = await node.Send(new CreateSharedDataCommand(new SharedDataCreateDto(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3)));

        await node.Send(new GetSharedDataQuery(created.Value!.Tag!));
        await node.Send(new GetSharedDataQuery(created.Value!.Tag!));

        Assert.Equal(0, await node.Database(context => context.SharedData.Select(item => item.AccessCount).SingleAsync()));
    }

    [Fact]
    public async Task An_unknown_tag_is_a_successful_lookup_without_a_value()
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new GetSharedDataQuery(Guid.NewGuid().ToString()));

        Assert.True(result.Success);
        Assert.Null(result.Value);
    }
}
