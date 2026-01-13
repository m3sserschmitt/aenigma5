/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using Enigma5.App.Models;

namespace Enigma5.App.UI;

public sealed class DashboardUIState
{
    private readonly object _inboundLock = new();
    private readonly object _outboundLock = new();
    private readonly object _keyLock = new();

    private HashSet<PeerDto> _inboundPeers = [];
    private HashSet<PeerDto> _outboundPeers = [];
    private bool _privateKeyUnlocked;

    public IReadOnlyCollection<PeerDto> InboundPeers => _inboundPeers;
    public IReadOnlyCollection<PeerDto> OutboundPeers => _outboundPeers;
    public bool PrivateKeyUnlocked => _privateKeyUnlocked;

    public event Func<IReadOnlyCollection<PeerDto>, Task>? InboundPeersChanged;
    public event Func<IReadOnlyCollection<PeerDto>, Task>? OutboundPeersChanged;
    public event Func<bool, Task>? PrivateKeyUnlockedChanged;

    public async Task SetInboundPeersAsync(IEnumerable<PeerDto> peers)
    {
        var newSet = peers.ToHashSet();
        bool changed;

        lock (_inboundLock)
        {
            changed = !_inboundPeers.SetEquals(newSet);
            if (changed)
            {
                _inboundPeers = newSet;
            }
        }

        if (changed)
        {
            await NotifyAsync(InboundPeersChanged, _inboundPeers);
        }
    }

    public async Task SetOutboundPeersAsync(IEnumerable<PeerDto> peers)
    {
        var newSet = peers.ToHashSet();
        bool changed;

        lock (_outboundLock)
        {
            changed = !_outboundPeers.SetEquals(newSet);
            if (changed)
            {
                _outboundPeers = newSet;
            }
        }

        if (changed)
        {
            await NotifyAsync(OutboundPeersChanged, _outboundPeers);
        }
    }

    public async Task SetPrivateKeyUnlockedAsync(bool unlocked)
    {
        bool changed;

        lock (_keyLock)
        {
            changed = unlocked != _privateKeyUnlocked;
            if (changed)
            {
                _privateKeyUnlocked = unlocked;
            }
        }

        if (changed)
        {
            await NotifyAsync(PrivateKeyUnlockedChanged, unlocked);
        }
    }

    private static async Task NotifyAsync<T>(Func<T, Task>? handlers, T value)
    {
        if (handlers is null)
        {
            return;
        }
        foreach (var handler in handlers.GetInvocationList())
        {
            await ((Func<T, Task>)handler)(value);
        }
    }
}
