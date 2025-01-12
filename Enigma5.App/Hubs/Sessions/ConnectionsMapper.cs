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

using Enigma5.App.Common.Utils;
using Enigma5.App.Hubs.Sessions.Contracts;

namespace Enigma5.App.Hubs.Sessions;

public class ConnectionsMapper : IReadOnlyConnectionsMapper
{
    private readonly object _locker = new();

    private readonly Dictionary<string, string> _connections = [];

    public IReadOnlyDictionary<string, string> Connections => _connections;

    public bool TryAdd(string address, string connectionId)
    => ThreadSafeExecution.Execute(() => _connections.TryAdd(address, connectionId), false, _locker);

    public bool Remove(string connectionId, out string? address)
    => ThreadSafeExecution.Execute(
        (out string? addr) =>
        {
            addr = null;
            
            foreach (var pair in _connections)
            {
                if (pair.Value == connectionId)
                {
                    addr = pair.Key;
                    break;
                }
            }

            if (addr == null || !_connections.Remove(addr, out var _))
            {
                return false;
            }

            return true;
        },
        false,
        out address,
        _locker
    );

    public bool TryGetConnectionId(string address, out string? connectionId)
    => ThreadSafeExecution.Execute(
        (out string? connId) => _connections.TryGetValue(address, out connId),
        false,
        out connectionId,
        _locker
    );

    public bool TryGetAddress(string connectionId, out string? address)
    => ThreadSafeExecution.Execute(
        (out string? addr) =>
        {
            try
            {
                var item = _connections.First(item => item.Value == connectionId);
                addr = item.Key;
                return true;
            }
            catch
            {
                addr = null;
                return false;
            }
        },
        false,
        out address,
        _locker
    );
}
