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

public class CleanupSharedDataHandlerTests
{
    private static SharedData Data(TimeSpan age) => new()
    {
        Data = TestSignedData.SharedPayloadSignedWithKey3,
        PublicKey = TestKeys.PublicKey3,
        Timestamp = (DateTimeOffset.UtcNow - age).ToUnixTimeSeconds()
    };

    [Fact]
    public async Task Shared_data_older_than_the_retention_period_is_removed()
    {
        await using var node = await TestNode.StartAsync();
        var old = Data(TimeSpan.FromDays(15));
        var recent = Data(TimeSpan.FromDays(13));
        await node.Database(async context =>
        {
            context.SharedData.AddRange(old, recent);
            await context.SaveChangesAsync();
        });

        var result = await node.Send(new CleanupSharedDataCommand(TimeSpan.FromDays(14)));

        Assert.True(result.Success);
        Assert.Equal(1, result.Value);
        Assert.Equal([recent.Tag], await node.Database(context => context.SharedData.Select(item => item.Tag).ToListAsync()));
    }

    [Fact]
    public async Task Nothing_is_removed_when_nothing_is_old_enough()
    {
        await using var node = await TestNode.StartAsync();
        await node.Database(async context =>
        {
            context.SharedData.Add(Data(TimeSpan.FromMinutes(1)));
            await context.SaveChangesAsync();
        });

        Assert.Equal(0, (await node.Send(new CleanupSharedDataCommand(TimeSpan.FromDays(14)))).Value);
    }
}
