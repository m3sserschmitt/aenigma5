using System.Collections.Concurrent;
using System.Net.Http.Json;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Security;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Network;

public class NetworkBridge
{
    private readonly CertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private readonly IMediator _commandRouter;

    private readonly ConcurrentDictionary<string, HubConnection> _connections;

    public NetworkBridge(CertificateManager certificateManager, IMediator commandRouter, IConfiguration configuration)
    {
        _certificateManager = certificateManager;
        _configuration = configuration;
        _commandRouter = commandRouter;
        _connections = new();
    }

    public async Task ConnectToNetworkAsync()
    {
        await StartAsync();
        await BroadcastAdjacencyListAsync();
    }

    private async Task StartAsync()
    {
        var peers = _configuration.GetSection("Peers").Get<List<string>>() ?? new();
        await ConnectPeersAsync(peers);
    }

    public async Task<bool> BroadcastAdjacencyListAsync()
    {
        var broadcastAdjacencyList = new BroadcastAdjacencyList(_connections.Keys.ToList(), _certificateManager, _configuration);

        return await BroadcastAdjacencyListAsync(broadcastAdjacencyList);
    }

    public async Task<bool> BroadcastAdjacencyListAsync(BroadcastAdjacencyList broadcastAdjacencyList)
    {
        try
        {
            foreach (var item in _connections)
            {
                await item.Value.InvokeAsync("Broadcast", broadcastAdjacencyList);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ConnectPeersAsync(IEnumerable<string> urls)
    {
        bool result = true;
        foreach (var url in urls)
        {
            result &= await ConnectPeerAsync(url);
        }

        await _commandRouter.Send(new UpdateNetworkGraphCommand
        {
            Vertex = Vertex.Create(_certificateManager, new List<string>(_connections.Keys), _configuration.GetValue<string>("Hostname"))
        });

        return result;
    }

    public async Task<bool> ConnectPeerAsync(string url, string? address = null)
    {
        var peerAddress = address ?? (await GetServerInfoAsync(url))?.Address;

        if (peerAddress == null)
        {
            return false;
        }

        bool result = false;

        if (peerAddress != null
        && !_connections.ContainsKey(peerAddress))
        {
            try
            {
                var connection = new HubConnectionBuilder()
                            .WithUrl($"{url.Trim('/')}/OnionRouting")
                            .Build();
                await connection.StartAsync();
                connection.Closed += async _ => { await OnHubConnectionClosed(peerAddress); };

                result = _connections.TryAdd(peerAddress, connection);
            }
            catch
            {
            }

        }

        return result;
    }

    public async Task<bool> CloseConnectionAsync(string address, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(address, out var connection))
        {
            await connection.StopAsync(cancellationToken);
            return true;
        }

        return false;
    }

    public async Task<bool> RouteMessageAsync(string destination, string content)
    {
        var destinationExists = _connections.TryGetValue(destination, out HubConnection? connection);

        if (destinationExists)
        {
            try
            {
                await connection!.InvokeAsync("RouteMessage", content);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private async Task OnHubConnectionClosed(string address)
    {
        _connections.Remove(address, out var _);

        await _commandRouter.Send(new UpdateNetworkGraphCommand
        {
            Vertex = Vertex.Create(_certificateManager, new List<string>(_connections.Keys), _configuration.GetValue<string>("Hostname"))
        });

        await BroadcastAdjacencyListAsync();
    }

    private static async Task<ServerInfo?> GetServerInfoAsync(string host)
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetAsync($"{host.Trim('/')}/ServerInfo");
            return await response.Content.ReadFromJsonAsync<ServerInfo>();
        }
        catch
        {
            return null;
        }
    }
}
