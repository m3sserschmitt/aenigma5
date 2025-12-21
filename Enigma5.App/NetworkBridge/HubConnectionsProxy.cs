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

using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using Enigma5.Crypto;
using Enigma5.Crypto.Extensions;
using Enigma5.Security.Contracts;
using Enigma5.Security.Extensions;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Enigma5.App.NetworkBridge;

public class HubConnectionsProxy(
    IConfiguration configuration,
    ICertificateManager certificateManager,
    IServiceScopeFactory serviceScopeFactory)
{
    private readonly IConfiguration _configuration = configuration;

    private HashSet<ConnectionVector> _connections = [];

    private readonly IServiceScopeFactory _scopeFactory = serviceScopeFactory;

    private readonly ICertificateManager _certificateManager = certificateManager;

    private HubConnection? _localHubConnection;

    internal event Func<Exception?, Task>? OnAnyTargetClosed
    {
        add
        {
            foreach (var connection in _connections)
            {
                connection.TargetClosed += value;
            }
        }
        remove
        {
            foreach (var connection in _connections)
            {
                connection.TargetClosed -= value;
            }
        }
    }

    internal async Task<bool> LoadConnections()
    {
        if (_connections.Count > 0)
        {
            return await ReloadConnections();
        }

        var listenAddress = _configuration.GetLocalListenAddress();
        if (string.IsNullOrWhiteSpace(listenAddress))
        {
            return false;
        }

        _localHubConnection ??= ConnectionVector.CreateHubConnection(listenAddress);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetPeersQuery());
        if (!result.IsSuccessNotNullResultValue())
        {
            return false;
        }

        var urls = result.Value!.Select(peer => peer.Host ?? string.Empty).ToList();
        if (urls.Count == 0)
        {
            return true;
        }

        _connections = ConnectionVector.CreateConnections(listenAddress, urls);

        foreach (var connection in _connections)
        {
            connection.ForwardCloseSignal();
            connection.ForwardMessageRouting();
            connection.ForwardBroadcasts();
        }
        return true;
    }

    internal Task<bool> StartAsync(CancellationToken cancellationToken = default) => _connections.StartAsync(cancellationToken);

    internal Task<bool> StartAuthenticationAsync(CancellationToken cancellationToken = default) => _connections.StartAuthenticationAsync(cancellationToken);

    internal Task<bool> StopAsync(CancellationToken cancellationToken = default) => _connections.StopAsync(cancellationToken);

    internal async Task<bool> TriggerBroadcast(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_localHubConnection != null && _localHubConnection.State != HubConnectionState.Connected)
            {
                await _localHubConnection.StartAsync(cancellationToken);
                await _localHubConnection.AuthenticateAsync(_certificateManager, cancellationToken);
            }

            var newAddresses = _connections
            .Where(item => item.TargetPublicKey.IsValidPublicKey())
            .Select(item => CertificateHelper.GetHexAddressFromPublicKey(item.TargetPublicKey)).ToList();
            var result = _localHubConnection != null ?
            await _localHubConnection.InvokeAsync<InvocationResultDto<bool>>(
                nameof(IEnigmaHub.TriggerBroadcast), new TriggerBroadcastRequestDto(newAddresses), cancellationToken: cancellationToken
            ) : null;
            return result?.Data ?? false;
        }
        catch (Exception)
        {
            // TODO: log exception;
            return false;
        }
    }

    private async Task<bool> ReloadConnections()
    {
        try
        {
            var newProxy = new HubConnectionsProxy(_configuration, _certificateManager, _scopeFactory);
            await newProxy.LoadConnections();

            var connectionsToBeRemoved = _connections.Except(newProxy._connections).ToList();
            _connections.IntersectWith(newProxy._connections);
            _connections.UnionWith(newProxy._connections);

            foreach (var connection in connectionsToBeRemoved)
            {
                await connection.StopAsync();
            }
            return true;
        }
        catch (Exception ex)
        {
            // TODO: log exception
            Console.WriteLine($"Exception while trying to reload connections: {ex.Message}.");
            return false;
        }
    }
}
