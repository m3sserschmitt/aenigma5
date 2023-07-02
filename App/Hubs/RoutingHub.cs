using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Contracts;
using Enigma5.App.Attributes;

namespace Enigma5.App.Hubs;

public class RoutingHub :
    RoutingHubBase<RoutingHub>,
    ICertificateValidationHub,
    IOnionParsingHub,
    IOnionRoutingHub
{
    public RoutingHub(IConnectionsMapper connectionsMapper) : base(connectionsMapper) { }

    public string? Address { get; set; }

    public string? DestinationConnectionId { get; set; }

    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    [CertificateValidation]
    public async Task ValidateCertificate(string certificatePem)
    {
        await RespondAsync(nameof(ValidateCertificate), Address != null);
    }

    [OnionParsing]
    [OnionRouting]
    public async Task RouteMessage(string data)
    {
        if (DestinationConnectionId != null && Content != null)
        {
            await SendAsync(DestinationConnectionId, Content);
        }
    }
}
