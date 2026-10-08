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

public class CreateSharedDataHandlerTests
{
    [Fact]
    public async Task Signed_data_is_stored_and_its_tag_address_and_expiry_are_returned()
    {
        await using var node = await TestNode.StartAsync(("SharedDataRetentionPeriod", "01.00:00:00"));
        var before = DateTimeOffset.UtcNow;

        var result = await node.Send(new CreateSharedDataCommand(new SharedDataCreateDto(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3, 3)));

        Assert.True(result.Success);
        var stored = await node.Database(context => context.SharedData.SingleAsync());
        Assert.Equal(stored.Tag, result.Value!.Tag);
        Assert.Equal($"http://node.example/Share?Tag={stored.Tag}", result.Value.ResourceUrl);
        Assert.InRange(result.Value.ValidUntil!.Value, before.AddDays(1), DateTimeOffset.UtcNow.AddDays(1));
        Assert.Equal(TestSignedData.SharedPayloadSignedWithKey3, stored.Data);
        Assert.Equal(TestKeys.PublicKey3, stored.PublicKey);
        Assert.Equal(3, stored.MaxAccessCount);
        Assert.Equal(0, stored.AccessCount);
    }

    [Fact]
    public async Task Without_an_access_count_the_data_can_be_read_once()
    {
        await using var node = await TestNode.StartAsync();

        await node.Send(new CreateSharedDataCommand(new SharedDataCreateDto(TestKeys.PublicKey3, TestSignedData.SharedPayloadSignedWithKey3, null)));

        Assert.Equal(1, await node.Database(context => context.SharedData.Select(item => item.MaxAccessCount).SingleAsync()));
    }

    [Theory]
    [MemberData(nameof(BadRequests))]
    public async Task Data_with_a_bad_key_or_signature_is_refused(string? publicKey, string? signedData)
    {
        await using var node = await TestNode.StartAsync();

        var result = await node.Send(new CreateSharedDataCommand(new SharedDataCreateDto(publicKey, signedData)));

        Assert.False(result.Success);
        Assert.Equal(0, await node.Database(context => context.SharedData.CountAsync()));
    }

    public static TheoryData<string?, string?> BadRequests => new()
    {
        { TestKeys.PublicKey1, TestSignedData.SharedPayloadSignedWithKey3 },
        { TestKeys.PublicKey3, "dGVzdC1zdHJpbmc=" },
        { TestKeys.PublicKey3, "not base64!" },
        { "not a key", TestSignedData.SharedPayloadSignedWithKey3 },
        { null, TestSignedData.SharedPayloadSignedWithKey3 },
        { TestKeys.PublicKey3, null }
    };
}
