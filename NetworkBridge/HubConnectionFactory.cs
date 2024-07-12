using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace NetworkBridge;

public class HubConnectionFactory(IConfiguration configuration)
{
    private readonly string _listenAddress = configuration.GetLocalListenAddress()
        ?? throw new Exception("Local listening address not provided into configuration file.");

    private List<ConnectionVector> Build(IEnumerable<string> urls)
    {
        return urls.Select(item =>
        {
            var local = new HubConnectionBuilder()
                                        .WithUrl($"{_listenAddress.Trim('/')}/{Endpoints.OnionRoutingEndpoint}")
                                        .Build();

            var remote = new HubConnectionBuilder()
                                .WithUrl($"{item.Trim('/')}/{Endpoints.OnionRoutingEndpoint}")
                                .Build();
            return new ConnectionVector(local, remote);
        }).ToList();
    }

    public HubConnectionsProxy CreateConnectionsProxy(IEnumerable<string> urls)
    {
        return new HubConnectionsProxy(Build(urls));
    }
}
