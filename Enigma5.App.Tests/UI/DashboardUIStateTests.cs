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

using Enigma5.App.Common.Utils;
using Enigma5.App.Models;
using Enigma5.App.UI;
using Enigma5.Tests.Base;

namespace Enigma5.App.Tests.UI;

public sealed class DashboardUIStateTests : IDisposable
{
    private readonly SimpleSingleThreadRunner _runner = new();

    private readonly CapturingLogger<DashboardUIState> _logger = new();

    private readonly DashboardUIState _state;

    public DashboardUIStateTests()
    {
        _state = new DashboardUIState(_runner, _logger);
    }

    public void Dispose() => _runner.Dispose();

    private static PeerDto Peer(int id) => new() { Id = id, Host = $"http://peer{id}.example", Address = id.ToString("x64") };

    // Waits until the notifications that were queued so far have been handed out.
    private Task Settle() => _runner.RunAsync(() => true);

    private static async Task Eventually(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }
        Assert.True(condition());
    }

    #region Values

    [Fact]
    public void A_new_state_is_locked_and_has_no_peers()
    {
        Assert.False(_state.PrivateKeyUnlocked);
        Assert.Empty(_state.InboundPeers);
        Assert.Empty(_state.OutboundPeers);
    }

    [Fact]
    public async Task A_setter_stores_the_value_and_tells_whether_it_changed()
    {
        Assert.True(await _state.SetPrivateKeyUnlockedAsync(true));
        Assert.True(await _state.SetInboundPeersAsync([Peer(1)]));
        Assert.True(await _state.SetOutboundPeersAsync([Peer(2), Peer(3)]));

        Assert.True(_state.PrivateKeyUnlocked);
        Assert.Equal([Peer(1)], _state.InboundPeers);
        Assert.Equal([Peer(2), Peer(3)], _state.OutboundPeers);
    }

    [Fact]
    public async Task Setting_an_equal_value_changes_nothing_and_notifies_nobody()
    {
        await _state.SetPrivateKeyUnlockedAsync(true);
        await _state.SetInboundPeersAsync([Peer(1), Peer(2)]);
        await Settle();
        var notifications = 0;
        _state.PrivateKeyUnlockedChanged += _ => { notifications++; return Task.CompletedTask; };
        _state.InboundPeersChanged += _ => { notifications++; return Task.CompletedTask; };

        Assert.False(await _state.SetPrivateKeyUnlockedAsync(true));
        Assert.False(await _state.SetInboundPeersAsync([Peer(2), Peer(1)]));
        await Settle();

        Assert.Equal(0, notifications);
    }

    #endregion

    #region Notifications

    [Fact]
    public async Task Every_subscriber_is_told_about_a_change()
    {
        var first = new List<bool>();
        var second = new List<bool>();
        _state.PrivateKeyUnlockedChanged += value => { lock (first) first.Add(value); return Task.CompletedTask; };
        _state.PrivateKeyUnlockedChanged += value => { lock (second) second.Add(value); return Task.CompletedTask; };

        await _state.SetPrivateKeyUnlockedAsync(true);

        await Eventually(() => first.Count == 1 && second.Count == 1);
        Assert.Equal([true], first);
        Assert.Equal([true], second);
    }

    [Fact]
    public async Task Changes_from_many_threads_are_notified_in_order_and_end_on_the_stored_value()
    {
        var seen = new List<bool>();
        _state.PrivateKeyUnlockedChanged += value => { lock (seen) seen.Add(value); return Task.CompletedTask; };

        await Task.WhenAll(Enumerable.Range(0, 8).Select(thread => Task.Run(async () =>
        {
            for (var index = 0; index < 25; index++)
            {
                await _state.SetPrivateKeyUnlockedAsync((index + thread) % 2 == 0);
            }
        })));
        await _state.SetPrivateKeyUnlockedAsync(!_state.PrivateKeyUnlocked);
        await Settle();
        await Task.Delay(50);

        bool[] notified;
        lock (seen) notified = [.. seen];
        // Only real changes are notified, so the values must alternate.
        Assert.All(notified.Zip(notified.Skip(1)), pair => Assert.NotEqual(pair.First, pair.Second));
        Assert.Equal(_state.PrivateKeyUnlocked, notified[^1]);
    }

    [Fact]
    public async Task A_slow_subscriber_delays_neither_the_caller_nor_the_other_subscribers()
    {
        var slowFinished = 0;
        var fast = 0;
        _state.InboundPeersChanged += async _ => { await Task.Delay(2000); Interlocked.Increment(ref slowFinished); };
        _state.InboundPeersChanged += _ => { Interlocked.Increment(ref fast); return Task.CompletedTask; };
        var started = DateTime.UtcNow;

        await _state.SetInboundPeersAsync([Peer(1)]);
        await _state.SetInboundPeersAsync([Peer(1), Peer(2)]);
        await _state.SetOutboundPeersAsync([Peer(3)]);

        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(1));
        await Eventually(() => fast == 2);
        Assert.Equal(0, slowFinished);
    }

    [Fact]
    public async Task A_failing_subscriber_does_not_stop_the_others_and_is_logged()
    {
        var after = 0;
        _state.OutboundPeersChanged += _ => throw new InvalidOperationException("thrown at once");
        _state.OutboundPeersChanged += _ => Task.FromException(new InvalidOperationException("failed task"));
        _state.OutboundPeersChanged += _ => { Interlocked.Increment(ref after); return Task.CompletedTask; };

        Assert.True(await _state.SetOutboundPeersAsync([Peer(1)]));

        await Eventually(() => after == 1 && _logger.Errors.Count == 2);
        // A subscriber that throws at once is logged as it is; a failed task arrives wrapped in an AggregateException.
        Assert.Contains(_logger.Errors, entry => entry.Exception is InvalidOperationException { Message: "thrown at once" });
        Assert.Contains(_logger.Errors, entry => entry.Exception?.GetBaseException() is InvalidOperationException { Message: "failed task" });
    }

    #endregion

    #region Copies

    [Fact]
    public async Task Readers_and_subscribers_get_copies_of_the_stored_peers()
    {
        IReadOnlyCollection<PeerDto>? notified = null;
        _state.InboundPeersChanged += peers => { notified = peers; return Task.CompletedTask; };
        var given = Peer(1);

        await _state.SetInboundPeersAsync([given]);
        await Eventually(() => notified is not null);
        given.Host = "changed by the caller";
        notified!.First().Connected = true;
        _state.InboundPeers.First().Host = "changed by a reader";

        var stored = Assert.Single(_state.InboundPeers);
        Assert.Equal("http://peer1.example", stored.Host);
        Assert.False(stored.Connected);
    }

    #endregion
}
