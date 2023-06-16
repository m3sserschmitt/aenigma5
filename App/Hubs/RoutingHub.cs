using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Contracts;
using Enigma5.App.Attributes;
using Enigma5.Message;

namespace Enigma5.App.Hubs;

public class RoutingHub : RoutingHubBase<RoutingHub>, ICertificateValidationHub, IOnionParserHub, IOnionRouterHub
{
    public RoutingHub(IConnectionsMapper connectionsMapper) : base(connectionsMapper) { }

    public string? Address { get; set; }

    public string? DestinationConnectionId { get; set; }

    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    //private bool Remove() => connectionsMapper.Remove(Context.ConnectionId);

    [CertificateValidation]
    public async Task ValidateCertificate(string certificatePem)
    {
        await RespondAsync(nameof(ValidateCertificate), Address != null);
    }

    [OnionParser]
    [OnionRouter]
    public async Task RouteMessage(string data)
    {
        if(DestinationConnectionId != null && Content != null)
        {
            await SendAsync(DestinationConnectionId, Content);
        }
    }

    /*public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Remove();
        await base.OnDisconnectedAsync(exception);
    }*/
}
