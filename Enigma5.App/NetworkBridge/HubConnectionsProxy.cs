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
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Models.Contracts.Hubs;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using Enigma5.Security.Contracts;
using Enigma5.Security.Extensions;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Enigma5.App.NetworkBridge;

public class HubConnectionsProxy(
    NetworkGraphValidationPolicy networkGraphValidationPolicy,
    IConfiguration configuration,
    ICertificateManager certificateManager,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<HubConnectionsProxy> logger)
{
    private readonly IConfiguration _configuration = configuration;

    private readonly NetworkGraphValidationPolicy _networkGraphValidationPolicy = networkGraphValidationPolicy;

    private HashSet<ConnectionVector> _connections = [];

    private readonly IServiceScopeFactory _scopeFactory = serviceScopeFactory;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly ILogger<HubConnectionsProxy> _logger = logger;

    public event Func<Exception?, ConnectionVector, Task>? OnAnyClosed
    {
        add
        {
            foreach (var connection in _connections)
            {
                connection.Closed += value;
            }
        }
        remove
        {
            foreach (var connection in _connections)
            {
                connection.Closed -= value;
            }
        }
    }

    public bool RemoveConnection(ConnectionVector connectionVector)
    {
        if (_connections.Remove(connectionVector))
        {
            _logger.LogDebug($"Connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}} removed successfully.", connectionVector);
            return true;
        }
        else
        {
            _logger.LogDebug($"Connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}} was not found, so it cannot be removed.", connectionVector);
            return false;
        }
    }

    public async Task<bool> LoadConnectionsAsync()
    {
        try
        {
            _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}...", nameof(LoadConnectionsAsync));
            if (_connections.Count > 0)
            {
                _logger.LogDebug("There are previous connections available. Resyncing with the database...");
                return await ReloadConnections();
            }

            var localAddress = _configuration.GetHttpEndpoint();
            if (string.IsNullOrWhiteSpace(localAddress))
            {
                _logger.LogError("Local listen address not configured.");
                return false;
            }

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new GetPeersQuery());
            if (!result.IsSuccessNotNullResultValue())
            {
                _logger.LogError("Could not retrieve peers.");
                return false;
            }

            if (result.Value?.Count == 0)
            {
                _logger.LogDebug("No peers found.");
                return true;
            }

            _connections = ConnectionVector.CreateConnections(
                localAddress,
                result.Value ?? [],
                _networkGraphValidationPolicy,
                _certificateManager,
                _configuration,
                _logger);

            foreach (var connection in _connections)
            {
                connection.ForwardMessageRouting();
                connection.ForwardBroadcasts();
                _logger.LogDebug($"Connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}} configured.", connection);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error encountered while invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}.", nameof(LoadConnectionsAsync));
            return false;
        }
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}...", nameof(StartAsync));
        return _connections.StartAsync(cancellationToken);
    }
    public Task<bool> StartAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}...", nameof(StartAuthenticationAsync));
        return _connections.StartAuthenticationAsync(cancellationToken);
    }

    public Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}...", nameof(StopAsync));
        return _connections.StopAsync(cancellationToken);
    }

    public async Task<bool> TriggerBroadcastAsync(CancellationToken cancellationToken = default)
    {
        HubConnection? localHubConnection = null;
        try
        {
            _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}...", nameof(TriggerBroadcastAsync));
            var newAddresses = _connections
            .Where(item => item.TargetAddress.IsValidAddress() && item.Authenticated)
            .Select(item => item.TargetAddress ?? string.Empty).ToList();
            localHubConnection = await GetLocalHubConnectionAsync(cancellationToken);
            if (localHubConnection == null)
            {
                _logger.LogError("Could not create connection to local Hub. Aborting...");
                return false;
            }
            var result = await localHubConnection.InvokeAsync<InvocationResultDto<bool>>(
                nameof(IEnigmaHub.TriggerBroadcast), new TriggerBroadcastRequestDto(newAddresses), cancellationToken: cancellationToken
            );
            return result?.Data ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error encountered while invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}.", nameof(TriggerBroadcastAsync));
            return false;
        }
        finally
        {
            if (localHubConnection != null)
            {
                await localHubConnection.StopAsync(cancellationToken);
            }
        }
    }

    private async Task<HubConnection?> GetLocalHubConnectionAsync(CancellationToken cancellationToken = default)
    {
        var localAddress = _configuration.GetControlHttpEndpoint();
        if (string.IsNullOrWhiteSpace(localAddress))
        {
            _logger.LogError("Local listen address not configured.");
            return null;
        }

        var localHubConnection = ConnectionVector.CreateHubConnection(localAddress);

        await localHubConnection.StartAsync(cancellationToken);
        if (!await localHubConnection.AuthenticateAsync(_certificateManager, cancellationToken))
        {
            _logger.LogError("Could not authenticate to local Hub. Aborting...");
            await localHubConnection.StopAsync(cancellationToken);
            return null;
        }
        return localHubConnection;
    }

    private async Task<bool> ReloadConnections()
    {
        try
        {
            _logger.LogDebug($"Invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}...", nameof(ReloadConnections));
            var newProxy = new HubConnectionsProxy(_networkGraphValidationPolicy, _configuration, _certificateManager, _scopeFactory, _logger);
            await newProxy.LoadConnectionsAsync();

            var connectionsToBeRemoved = _connections.Except(newProxy._connections).ToList();
            _connections.IntersectWith(newProxy._connections);
            _connections.UnionWith(newProxy._connections);

            foreach (var connection in connectionsToBeRemoved)
            {
                _logger.LogDebug($"Closing connection vector {{{Common.Constants.Serilog.ConnectionVectorKey}}}...", connection);
                await connection.StopAsync();
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while invoking {{{Common.Constants.Serilog.HubConnectionsProxyMethodNameKey}}}.", nameof(ReloadConnections));
            return false;
        }
    }
}
