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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Data;
using Enigma5.App.UI;
using Enigma5.Security.Contracts;

namespace Enigma5.App.NetworkBridge;

public class Bridge(
    IConfiguration configuration,
    HubConnectionsProxy hubConnectionsProxy,
    SimpleSingleThreadRunner singleThreadRunner,
    ICertificateManager certificateManager,
    NetworkGraph networkGraph,
    DashboardUIState dashboardUIState,
    ILogger<Bridge> logger) : IDisposable
{
    private bool _disposed;

    private readonly HubConnectionsProxy _connections = hubConnectionsProxy;

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger _logger = logger;

    private readonly SimpleSingleThreadRunner _singleThreadRunner = singleThreadRunner;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly DashboardUIState _dashboardUIState = dashboardUIState;

    ~Bridge()
    {
        Dispose(false);
    }

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default) => await _singleThreadRunner.RunAsync(async () =>
    {
        _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.BridgeMethodNameKey}}}...", nameof(StartAsync));
        if (!await IsKeyAvailableAsync())
        {
            // Without the key no vector can sign in. Creating one would only fail and be retried without end.
            await _connections.StopAsync(cancellationToken);
            return false;
        }
        var result = await _connections.LoadConnectionsAsync(cancellationToken);
        RegisterEvents();
        result &= await _connections.StartAsync(cancellationToken);
        result &= await _connections.StartAuthenticationAsync(cancellationToken);
        result &= await _connections.TriggerBroadcastAsync(cancellationToken);
        result &= await _connections.SyncMessagesAsync(cancellationToken);
        result &= await _connections.CleanupAsync(cancellationToken);
        return result;
    }, _logger);

    // Asks the key itself, by signing. If the answer differs from what the node shows (for example
    // because the key file was locked or unlocked from outside), the local vertex and the dashboard follow.
    private async Task<bool> IsKeyAvailableAsync()
    {
        var available = await _certificateManager.CanSignAsync();
        if (available != _dashboardUIState.PrivateKeyUnlocked)
        {
            _logger.LogWarning("The private key is {State}. The local vertex is generated again.", available ? "available again" : "no longer available; connections to peers are stopped");
            await _networkGraph.GenerateLocalVertexAsync();
            await _dashboardUIState.SetPrivateKeyUnlockedAsync(available);
        }
        return available;
    }

    private void RegisterEvents()
    {
        _connections.OnAnyClosed -= OnConnectionClosedAsync;
        _connections.OnAnyClosed += OnConnectionClosedAsync;
    }

    private Task<bool> RemoveConnectionAsync(ConnectionVector connectionVector) => _singleThreadRunner.RunAsync(async () =>
    {
        _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.BridgeMethodNameKey}}} for connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}}...", nameof(RemoveConnectionAsync), connectionVector);
        // A bridge run that was waiting before this removal may have started the vector again. It is then kept.
        if (connectionVector.Connected)
        {
            _logger.LogDebug($"Connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}} is connected again, so it is not removed.", connectionVector);
            return false;
        }
        var removed = _connections.RemoveConnection(connectionVector);
        // A vector that leaves the bridge is always stopped, so that none of its connections stays open untracked.
        await connectionVector.StopAsync();
        return removed;
    }, _logger);

    private async Task OnConnectionClosedAsync(Exception? ex, ConnectionVector connectionVector)
    {
        _logger.LogError(ex, $"Invoking {{{Common.Constants.Serilog.BridgeMethodNameKey}}} for connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}} with exception.", nameof(OnConnectionClosedAsync), connectionVector);
        await RemoveConnectionAsync(connectionVector);
        {
            await Task.Delay(_configuration.GetDelayBetweenConnectionRetries());
            try
            {
                if (await StartAsync())
                {
                    _logger.LogDebug($"Invocation of {{{Common.Constants.Serilog.BridgeMethodNameKey}}} completed successfully. All connections were successfully established.", nameof(StartAsync));
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
