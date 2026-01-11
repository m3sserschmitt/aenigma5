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

public class DashboardUIState
{
    private HashSet<PeerDto> _inboundPeers = [];

    private HashSet<PeerDto> _outboundPeers = [];

    private bool _privateKeyUnlocked = false;

    public HashSet<PeerDto> InboundPeers
    {
        get => _inboundPeers;
        set
        {
            var valueSet = new HashSet<PeerDto>(value);
            lock (_inboundPeers)
            {
                if (valueSet.SetEquals(_inboundPeers))
                {
                    return;
                }
                _inboundPeers = valueSet;
            }
            OnInboundPeersChanged?.Invoke(_inboundPeers);
        }
    }

    public HashSet<PeerDto> OutboundPeers
    {
        get => _outboundPeers;
        set
        {
            var valueSet = new HashSet<PeerDto>(value);
            lock (_outboundPeers)
            {
                if (valueSet.SetEquals(_outboundPeers))
                {
                    return;
                }
                _outboundPeers = valueSet;
            }
            OnOutboundPeersChanged?.Invoke(_outboundPeers);
        }
    }

    public bool PrivateKeyUnlocked
    {
        get => _privateKeyUnlocked;
        set
        {
            if(value != _privateKeyUnlocked)
            {
                _privateKeyUnlocked = value;
                OnPrivateKeyUnlockedChanged?.Invoke(_privateKeyUnlocked);
            }
        }
    }

    public event Action<HashSet<PeerDto>>? OnInboundPeersChanged;

    public event Action<HashSet<PeerDto>>? OnOutboundPeersChanged;

    public event Action<bool>? OnPrivateKeyUnlockedChanged;
}
