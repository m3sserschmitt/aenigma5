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

using System.Text.RegularExpressions;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Federation;

// Node A (key 1) holds a backlog of messages for node B (key 2) before B ever connects. Then B starts with
// A as its peer, and its bridge moves the backlog. A logs at Debug, so that the hub calls of B can be counted.
public class BacklogFixture : IAsyncLifetime
{
    public const int Messages = 2 * Common.Constants.MessagesPageSize + 5;

    public NodeProcess A { get; private set; } = null!;

    public NodeProcess B { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        A = await NodeProcess.StartAAsync(("Serilog:MinimumLevel:Default", "Debug"));
        await using (var sender = await HubClient.SignedInAsync(A.PublicUrl, TestKeys.PrivateKey3))
        {
            for (var sent = 0; sent < Messages; sent += Common.Constants.MessagesPageSize)
            {
                var batch = Enumerable.Repeat<string?>(TestOnions.ForClientThroughAThenB, Math.Min(Common.Constants.MessagesPageSize, Messages - sent)).ToArray();
                Assert.True((await sender.Route(null, batch)).Success);
            }
        }
        Assert.Equal(Messages, A.Pending(TestKeys.Address2));

        B = await NodeProcess.StartBAsync();
        B.AddPeer(A);
        await B.RestartAsync();
    }

    public async Task DisposeAsync()
    {
        await B.DisposeAsync();
        await A.DisposeAsync();
    }
}

public class BacklogTests(BacklogFixture fixture) : IClassFixture<BacklogFixture>
{
    private readonly NodeProcess _a = fixture.A;

    private readonly NodeProcess _b = fixture.B;

    private Task<bool> BacklogMoved() => NodeProcess.Eventually(
        () => Task.FromResult(_a.Pending(TestKeys.Address2) == 0 && _b.Pending(TestKeys.Address3) == BacklogFixture.Messages),
        seconds: 60);

    [Fact]
    public async Task Every_message_of_the_backlog_arrives_once_and_is_confirmed_on_the_node_that_held_it()
    {
        Assert.True(await BacklogMoved(), $"The backlog was not moved. Log of B:{Environment.NewLine}{_b.Log}");

        Assert.Equal(BacklogFixture.Messages, _b.Count("SELECT COUNT(*) FROM Messages WHERE Destination = $d", ("$d", TestKeys.Address3)));
        Assert.Equal(BacklogFixture.Messages, _a.Count("SELECT COUNT(*) FROM Messages WHERE Destination = $d AND Sent = 1", ("$d", TestKeys.Address2)));
    }

    // The bridge of B confirms each page on A as soon as it has routed it, not once after the last page. A logs
    // every hub call with its argument: Pull2 with InfId, the last id before the page, and Cleanup2 with SupId.
    [Fact]
    public async Task The_backlog_is_confirmed_page_by_page()
    {
        Assert.True(await BacklogMoved());

        var calls = Regex.Matches(_a.Log, @"Invoking (Pull2|Cleanup2) for connectionId \S+ with the following data: \[\{""(?:InfId|SupId)"":(null|\d+)")
            .Select(match => (Method: match.Groups[1].Value, Id: match.Groups[2].Value == "null" ? (long?)null : long.Parse(match.Groups[2].Value)))
            .ToList();
        var confirmed = calls.Where(call => call.Method == "Cleanup2").Select(call => call.Id).ToList();

        // The last message of each page, from A's own records: ids 20, 40 and 45 on a fresh database.
        var pages = (BacklogFixture.Messages + Common.Constants.MessagesPageSize - 1) / Common.Constants.MessagesPageSize;
        var lastOfEachPage = Enumerable.Range(1, pages)
            .Select(page => (long?)_a.Count("SELECT Id FROM Messages WHERE Destination = $d ORDER BY Id LIMIT 1 OFFSET $o",
                ("$d", TestKeys.Address2), ("$o", Math.Min(page * Common.Constants.MessagesPageSize, BacklogFixture.Messages) - 1)))
            .ToList();
        Assert.Equal(lastOfEachPage, confirmed);

        // Each page is confirmed before the next one is pulled: every Cleanup2 is followed by a Pull2 that continues
        // after the confirmed page.
        Assert.All(calls.Select((call, index) => (call, index)).Where(item => item.call.Method == "Cleanup2"), item =>
        {
            Assert.True(item.index + 1 < calls.Count, $"Nothing was pulled after the confirmation of {item.call.Id}.");
            Assert.Equal(("Pull2", item.call.Id), calls[item.index + 1]);
        });
    }
}
