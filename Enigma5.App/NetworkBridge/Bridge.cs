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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;

namespace Enigma5.App.NetworkBridge;

public class Bridge(IConfiguration configuration, HubConnectionsProxy hubConnectionsProxy) : IDisposable
{
    private bool _disposed;

    private readonly HubConnectionsProxy _connections = hubConnectionsProxy;

    private readonly IConfiguration _configuration = configuration;

    private readonly SimpleSingleThreadRunner _singleThreadRunner = new();

    ~Bridge()
    {
        Dispose(false);
    }

    public async Task<bool> StartAsync() => await _singleThreadRunner.RunAsync(async () =>
    await _connections.LoadConnections() &&
        RegisterEvents() &&
        await _connections.StartAsync() &&
        await _connections.StartAuthenticationAsync() &&
        await _connections.TriggerBroadcast()
    );

    private bool RegisterEvents()
    {
        _connections.OnAnyClosed += OnConnectionClosedAsync;
        return true;
    }

    private Task<bool> RemoveConnection(ConnectionVector connectionVector)
    => _singleThreadRunner.RunAsync(() => _connections.RemoveConnection(connectionVector));

    private async Task OnConnectionClosedAsync(Exception? ex, ConnectionVector connectionVector)
    {
        await RemoveConnection(connectionVector);
        for (int i = 0; i < _configuration.GetConnectionRetriesCount(); i++)
        {
            await Task.Delay(_configuration.GetDelayBetweenConnectionRetries());
            try
            {
                if (await StartAsync())
                {
                    break;
                }
            }
            catch (Exception)
            {
                // TODO: Log failed attempt
                Console.WriteLine("Failed attempt to reestablish connection.");
                continue;
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {

            }
            _connections.OnAnyClosed -= OnConnectionClosedAsync;
            _disposed = true;
        }
    }
}
