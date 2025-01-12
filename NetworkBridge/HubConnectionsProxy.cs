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
using Enigma5.Crypto;
using Enigma5.Crypto.Extensions;
using Enigma5.Security;
using Enigma5.Security.Extensions;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetworkBridge;

public class HubConnectionsProxy
{
    private readonly ConfigurationLoader _configurationLoader;

    private readonly HashSet<ConnectionVector> _connections;

    private readonly CertificateManager _certificateManager;

    private readonly HubConnection _localHubConnection;

    public event Action? OnConnectionsReloaded;

    public event Func<Exception?, Task> OnAnyTargetClosed
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

    public event Func<Exception?, Task> OnAnySourceClosed
    {
        add
        {
            foreach (var connection in _connections)
            {
                connection.SourceClosed += value;
            }
        }
        remove
        {
            foreach (var connection in _connections)
            {
                connection.SourceClosed -= value;
            }
        }
    }

    private HubConnectionsProxy(
        ConfigurationLoader configurationLoader,
        CertificateManager certificateManager)
    {
        var listenAddress = configurationLoader.Configuration.GetLocalListenAddress()
        ?? throw new Exception("Local listening address not provided into configuration file.");
        List<string> urls = configurationLoader.Configuration.GetPeers()
        ?? throw new Exception("Peers section not provided into configuration.");

        _connections = ConnectionVector.CreateConnections(listenAddress, urls);
        _localHubConnection = ConnectionVector.CreateHubConnection(listenAddress);

        foreach (var connection in _connections)
        {
            connection.ForwardCloseSignal();
            connection.ForwardMessageRouting();
            connection.ForwardBroadcasts();
        }

        _certificateManager = certificateManager;
        _configurationLoader = configurationLoader;
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default) => _connections.StartAsync(cancellationToken);

    public Task<bool> StartAuthenticationAsync(CancellationToken cancellationToken = default) => _connections.StartAuthenticationAsync(cancellationToken);

    public Task<bool> StopAsync(CancellationToken cancellationToken = default) => _connections.StopAsync(cancellationToken);

    public async Task<bool> TriggerBroadcast(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_localHubConnection.State == HubConnectionState.Disconnected)
            {
                await _localHubConnection.StartAsync(cancellationToken);

                if (!await _localHubConnection.AuthenticateAsync(_certificateManager, cancellationToken))
                {
                    return false;
                }

                var newAddresses = _connections
                .Where(item => item.TargetPublicKey.IsValidPublicKey())
                .Select(item => CertificateHelper.GetHexAddressFromPublicKey(item.TargetPublicKey)).ToList();
                var requestModel = new TriggerBroadcastRequest(newAddresses);
                var result = await _localHubConnection.InvokeAsync<InvocationResult<bool>>(nameof(IEnigmaHub.TriggerBroadcast), requestModel, cancellationToken: cancellationToken);

                if (!result.Success && result.Data)
                {
                    Console.WriteLine($"Possible errors returned from server while invoking {nameof(TriggerBroadcast)} method. Server message: {string.Join(", ", result.Errors.Select(item => item.Message))}");
                }

                return result.Data;
            }
            return false;
        }
        catch (Exception)
        {
            // TODO: log exception;
            return false;
        }
        finally
        {
            if (_localHubConnection.State != HubConnectionState.Disconnected)
            {
                await _localHubConnection.StopAsync(CancellationToken.None);
            }
        }
    }

    private void ReloadConnections()
    {
        try
        {
            var newProxy = new HubConnectionsProxy(_configurationLoader, _certificateManager);
            var connectionsToBeRemoved = _connections.Except(newProxy._connections).ToList();

            _connections.IntersectWith(newProxy._connections);
            _connections.UnionWith(newProxy._connections);

            _ = Task.Run(() => { OnConnectionsReloaded?.Invoke(); });
            Parallel.ForEach(connectionsToBeRemoved, async connection => await connection.StopAsync());
        }
        catch (Exception ex)
        {
            // TODO: log exception
            Console.WriteLine($"Exception while trying to reload connections: {ex.Message}.");
        }
    }

    public static HubConnectionsProxy Create(ConfigurationLoader configurationLoader)
    {
        var certificateManager = new CertificateManager(new KeysReader(new CommandLinePassphraseReader(), configurationLoader.Configuration));

        var proxy = new HubConnectionsProxy(configurationLoader, certificateManager);
        configurationLoader.OnConfigurationReloaded += proxy.ReloadConnections;

        return proxy;
    }
}
