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

public class Bridge(IConfiguration configuration, HubConnectionsProxy hubConnectionsProxy, ILogger<Bridge> logger) : IDisposable
{
    private bool _disposed;

    private readonly HubConnectionsProxy _connections = hubConnectionsProxy;

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger _logger = logger;

    private readonly SimpleSingleThreadRunner _singleThreadRunner = new();

    ~Bridge()
    {
        Dispose(false);
    }

    public async Task<bool> StartAsync() => await _singleThreadRunner.RunAsync(async () =>
    {
        _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.BridgeMethodNameKey}}}...", nameof(StartAsync));
        var result = await _connections.LoadConnectionsAsync();
        RegisterEvents();
        result &= await _connections.StartAsync();
        result &= await _connections.StartAuthenticationAsync();
        result &= await _connections.TriggerBroadcastAsync();
        return result;
    }, _logger);

    private void RegisterEvents()
    {
        _connections.OnAnyClosed -= OnConnectionClosedAsync;
        _connections.OnAnyClosed += OnConnectionClosedAsync;
    }

    private Task<bool> RemoveConnectionAsync(ConnectionVector connectionVector) => _singleThreadRunner.RunAsync(() =>
    {
        _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.BridgeMethodNameKey}}} for connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}}...", nameof(RemoveConnectionAsync), connectionVector);
        return _connections.RemoveConnection(connectionVector);
    }, _logger);

    private async Task OnConnectionClosedAsync(Exception? ex, ConnectionVector connectionVector)
    {
        _logger.LogError(ex, $"Invoking {{{Common.Constants.Serilog.BridgeMethodNameKey}}} for connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}} with exception.", nameof(OnConnectionClosedAsync), connectionVector);
        await RemoveConnectionAsync(connectionVector);
        /* for (int i = 0; i < _configuration.GetConnectionRetriesCount(); i++) */
        {
            await Task.Delay(_configuration.GetDelayBetweenConnectionRetries());
            try
            {
                if (await StartAsync())
                {
                    _logger.LogDebug($"Invocation of {{{Common.Constants.Serilog.BridgeMethodNameKey}}} completed successfully. All connections were successfully established.", nameof(StartAsync));
                    // break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Exception encountered while invoking {{{Common.Constants.Serilog.BridgeMethodNameKey}}}. Retrying...", nameof(StartAsync));
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
