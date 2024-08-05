using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Models;
using Enigma5.App.Models.Extensions;
using Enigma5.Crypto;
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

    public Task<bool> StartAsync() => _connections.StartAsync();

    public Task<bool> StartAuthenticationAsync() => _connections.StartAuthenticationAsync();

    public Task<bool> StopAsync() => _connections.StopAsync();

    public async Task<bool> TriggerBroadcast()
    {
        try
        {
            if (_localHubConnection.State == HubConnectionState.Disconnected)
            {
                await _localHubConnection.StartAsync();

                if (!await _localHubConnection.AuthenticateAsync(_certificateManager, false, false))
                {
                    return false;
                }

                var newAddresses = _connections
                .Where(item => item.TargetPublicKey.IsValidPublicKey())
                .Select(item => CertificateHelper.GetHexAddressFromPublicKey(item.TargetPublicKey)).ToList();
                var requestModel = new TriggerBroadcastRequest(newAddresses);
                var authentication = await _localHubConnection.InvokeAsync<InvocationResult<bool>>(nameof(IHub.TriggerBroadcast), requestModel);
                return authentication.Success && authentication.Data;
            }
            return true;
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
                await _localHubConnection.StopAsync();
            }
        }
    }

    private async void ReloadConnections()
    {
        try
        {
            var newProxy = new HubConnectionsProxy(_configurationLoader, _certificateManager);
            var connectionsToBeRemoved = _connections.Except(newProxy._connections).ToList();

            _connections.IntersectWith(newProxy._connections);
            _connections.UnionWith(newProxy._connections);

            foreach (var connection in connectionsToBeRemoved)
            {
                await connection.StopAsync();
            }
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
