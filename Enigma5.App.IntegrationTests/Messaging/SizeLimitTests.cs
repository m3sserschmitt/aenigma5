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

using Enigma5.Tests.Base;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.IntegrationTests.Messaging;

public class SizeLimitTests(NodeAFixture fixture) : IClassFixture<NodeAFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    private Task<HubClient> SenderAsync() => HubClient.SignedInAsync(_node.PublicUrl, TestKeys.PrivateKey2);

    private static string?[] Repeat(string onion, int count) => [.. Enumerable.Repeat<string?>(onion, count)];

    [Fact]
    public async Task Twenty_onions_of_nearly_the_largest_size_fit_in_one_request()
    {
        await using var sender = await SenderAsync();
        var before = _node.Pending(TestKeys.Address3);

        var result = await sender.Route(null, Repeat(TestOnions.LargeForClientThroughA, 20));

        Assert.True(result.Success, result.Error);
        Assert.Equal(before + 20, _node.Pending(TestKeys.Address3));
    }

    [Fact]
    public async Task A_request_with_more_than_twenty_onions_is_refused()
    {
        await using var sender = await SenderAsync();

        var result = await sender.Route(null, Repeat(TestOnions.ForClientThroughA1, 21));

        Assert.False(result.Success);
        Assert.Equal("Too many payloads for one request.", result.Error);
    }

    [Fact]
    public async Task An_onion_above_the_largest_size_is_refused()
    {
        await using var sender = await SenderAsync();

        var result = await sender.Route(null, new string('A', 16388));

        Assert.False(result.Success);
        Assert.Equal("One or more payloads exceed the maximum onion size.", result.Error);
    }

    // The hub closes a connection that sends more than its message limit; the caller sees the call fail.
    [Fact]
    public async Task A_message_above_the_limit_of_the_hub_ends_the_connection()
    {
        await using var sender = await SenderAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => sender.Route(null, Repeat(new string('A', 17000), 20)));

        await using var next = await SenderAsync();
        Assert.True((await next.Route(null, TestOnions.ForClientThroughA1)).Success);
    }
}
