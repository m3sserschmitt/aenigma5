using System.Net.Http.Json;
using Enigma5.App.Models;
using Enigma5.App.Security;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App.Network;

public class NetworkBridge : Dictionary<string, HubConnection>
{
    private readonly CertificateManager _certificateManager;

    private readonly IConfiguration _configuration;

    private readonly Mutex _mutex = new();

    public NetworkBridge(CertificateManager certificateManager, IConfiguration configuration)
    {
        _certificateManager = certificateManager;
        _configuration = configuration;
    }

    public async Task StartAsync()
    {
        var peers = _configuration.GetSection("Peers").Get<List<string>>() ??
        new();

        foreach (var peer in peers)
        {
            var serverInfo = await GetServerInfoAsync(peer);
            if (serverInfo != null)
            {
                var connection = new HubConnectionBuilder()
                .WithUrl($"{peer.Trim('/')}/OnionRouting")
                .Build();
                await connection.StartAsync();

                _mutex.WaitOne();
                TryAdd(serverInfo.Address!, connection);
                _mutex.ReleaseMutex();
            }
        }
    }

    public async Task BroadcastAdjacencyListAsync()
    {
        try
        {
            _mutex.WaitOne();
            var broadcastAdjacencyList = new BroadcastAdjacencyList(Keys.ToList(), _certificateManager, _configuration);

            foreach (var item in this)
            {
                await item.Value.InvokeAsync("Broadcast", broadcastAdjacencyList);
            }
        }
        catch
        {
            _mutex.ReleaseMutex();
        }
    }

    public async Task ConnectToNetworkAsync()
    {
        await StartAsync();
        await BroadcastAdjacencyListAsync();
    }

    public async Task<bool> RouteMessageAsync(string destination, string content)
    {
        _mutex.WaitOne();
        var destinationExists = TryGetValue(destination, out HubConnection? connection);

        if (destinationExists)
        {
            try
            {
                await connection!.InvokeAsync("RouteMessage", content);
            }
            catch
            {
                _mutex.ReleaseMutex();
                return true;
            }
        }

        _mutex.ReleaseMutex();

        return false;
    }

    private static async Task<ServerInfo?> GetServerInfoAsync(string host)
    {
        var client = new HttpClient();
        var response = await client.GetAsync($"{host.Trim('/')}/ServerInfo");
        return await response.Content.ReadFromJsonAsync<ServerInfo>();
    }
}
