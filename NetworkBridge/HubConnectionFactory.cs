using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace NetworkBridge;

public class HubConnectionFactory
{
    private readonly string _listenAddress;

    public HubConnectionFactory(IConfiguration configuration)
    {
        _listenAddress = configuration.GetLocalListenAddress()
        ?? throw new Exception("Local listening address not provided into configuration file.");
    }

    private IList<(HubConnection local, HubConnection remote)> Build(IList<string> urls)
    {
        var result = new List<(HubConnection local, HubConnection remote)>();
        foreach (var url in urls)
        {
            var local = new HubConnectionBuilder()
                                        .WithUrl($"{_listenAddress.Trim('/')}/{Endpoints.OnionRoutingEndpoint}")
                                        .Build();

            var remote = new HubConnectionBuilder()
                                .WithUrl($"{url.Trim('/')}/{Endpoints.OnionRoutingEndpoint}")
                                .Build();

            result.Add((local, remote));
        }
        return result;
    }

    public HubConnectionProxy Create(IList<string> urls)
    {
        return new HubConnectionProxy(Build(urls));
    }
}
