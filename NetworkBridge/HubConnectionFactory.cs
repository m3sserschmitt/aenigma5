using Enigma5.App.Common.Extensions;
using Enigma5.App.Common.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Enigma5.Security;

namespace NetworkBridge;

public class HubConnectionFactory(IConfiguration configuration)
{
    private readonly string _listenAddress = configuration.GetLocalListenAddress()
        ?? throw new Exception("Local listening address not provided into configuration file.");

    private readonly List<string> _urls = configuration.GetPeers()
    ?? throw new Exception("Peers section not provided into configuration.");

    public HubConnectionsProxy CreateConnectionsProxy()
    {
        var _certificateManager = new CertificateManager(new KeysReader(new CommandLinePassphraseReader(), configuration));

        return new HubConnectionsProxy(CreateConnections(), CreateLocalHubConnection(), _certificateManager);
    }

    private List<ConnectionVector> CreateConnections()
    => _urls.Select(item => new ConnectionVector(CreateLocalHubConnection(), CreateHubConnection(item))).ToList();

    private HubConnection CreateLocalHubConnection() => CreateHubConnection(_listenAddress);

    private static HubConnection CreateHubConnection(string baseUrl)
    => new HubConnectionBuilder()
        .WithUrl($"{baseUrl.Trim('/')}/{Endpoints.OnionRoutingEndpoint}")
        .Build();
}
