using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Enigma5.App.Models.Extensions;
using Enigma5.Crypto;
using Enigma5.Security;
using Enigma5.Security.Extensions;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetworkBridge;

public class HubConnectionsProxy
{
    private readonly List<ConnectionVector> _connections;

    private readonly CertificateManager _certificateManager;

    private readonly HubConnection _localHubConnection;

    public event Func<Exception?, Task> OnAnyTargetClosed
    {
        add
        {
            _connections.ForEach(connection => connection.TargetClosed += value);
        }
        remove
        {
            _connections.ForEach(connection => connection.TargetClosed -= value);
        }
    }

    public event Func<Exception?, Task> OnAnySourceClosed
    {
        add
        {
            _connections.ForEach(connection => connection.SourceClosed += value);
        }
        remove
        {
            _connections.ForEach(connection => connection.SourceClosed -= value);
        }
    }

    public HubConnectionsProxy(
        List<ConnectionVector> connections,
        HubConnection localHubConnection,
        CertificateManager certificateManager)
    {
        _connections = connections;

        foreach (var connection in _connections)
        {
            connection.ForwardCloseSignal();
            connection.ForwardMessageRouting();
            connection.ForwardBroadcasts();
        }

        _certificateManager = certificateManager;
        _localHubConnection = localHubConnection;
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
                .Select(item => CertificateHelper.GetHexAddressFromPublicKey(item.TargetPublicKey)).ToHashSet();
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
}
