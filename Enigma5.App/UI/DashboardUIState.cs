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

namespace Enigma5.App.UI;

public sealed class DashboardUIState(SimpleSingleThreadRunner singleThreadRunner, ILogger<DashboardUIState> logger)
{
    private readonly SimpleSingleThreadRunner _singleThreadRunner = singleThreadRunner;

    private readonly ILogger _logger = logger;

    // Written only by work items of the runner. A stored set is never changed, only replaced.
    private volatile HashSet<PeerDto> _inboundPeers = [];

    private volatile HashSet<PeerDto> _outboundPeers = [];

    private volatile bool _privateKeyUnlocked;

    public IReadOnlyCollection<PeerDto> InboundPeers => Copy(_inboundPeers);

    public IReadOnlyCollection<PeerDto> OutboundPeers => Copy(_outboundPeers);

    public bool PrivateKeyUnlocked => _privateKeyUnlocked;

    public event Func<IReadOnlyCollection<PeerDto>, Task>? InboundPeersChanged;

    public event Func<IReadOnlyCollection<PeerDto>, Task>? OutboundPeersChanged;

    public event Func<bool, Task>? PrivateKeyUnlockedChanged;

    public Task<bool> SetInboundPeersAsync(IEnumerable<PeerDto> peers)
    {
        var newSet = Copy(peers);
        return _singleThreadRunner.RunAsync(() =>
        {
            if (_inboundPeers.SetEquals(newSet))
            {
                return false;
            }
            _inboundPeers = newSet;
            QueueNotification(() => InboundPeersChanged, () => Copy(newSet));
            return true;
        }, _logger);
    }

    public Task<bool> SetOutboundPeersAsync(IEnumerable<PeerDto> peers)
    {
        var newSet = Copy(peers);
        return _singleThreadRunner.RunAsync(() =>
        {
            if (_outboundPeers.SetEquals(newSet))
            {
                return false;
            }
            _outboundPeers = newSet;
            QueueNotification(() => OutboundPeersChanged, () => Copy(newSet));
            return true;
        }, _logger);
    }

    public Task<bool> SetPrivateKeyUnlockedAsync(bool unlocked)
    => _singleThreadRunner.RunAsync(() =>
    {
        if (_privateKeyUnlocked == unlocked)
        {
            return false;
        }
        _privateKeyUnlocked = unlocked;
        QueueNotification(() => PrivateKeyUnlockedChanged, () => unlocked);
        return true;
    }, _logger);

    // Notifications run as a work item of their own, so the caller of a setter waits only until the value is stored.
    // They are queued in the order in which the values were stored and are therefore delivered in that order.
    private void QueueNotification<T>(Func<Func<T, Task>?> getHandlers, Func<T> createValue)
    => _ = _singleThreadRunner.RunAsync(() =>
    {
        foreach (var handler in getHandlers()?.GetInvocationList().Cast<Func<T, Task>>() ?? [])
        {
            try
            {
                // Not awaited: a slow or failing subscriber must not hold up the runner or the other subscribers.
                _ = handler(createValue()).ContinueWith(
                    task => _logger.LogError(task.Exception, "A dashboard subscriber failed while handling a state change."),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A dashboard subscriber failed while handling a state change.");
            }
        }
        return true;
    }, _logger);

    // Subscribers and readers get their own objects, so nothing they change reaches the stored state.
    private static HashSet<PeerDto> Copy(IEnumerable<PeerDto> peers)
    => [.. peers.Select(item => new PeerDto { Id = item.Id, Host = item.Host, Address = item.Address, Connected = item.Connected })];
}
