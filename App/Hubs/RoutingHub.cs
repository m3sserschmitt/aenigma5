using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.MemoryStorage;
using Enigma5.App.MemoryStorage.Contracts;

namespace Enigma5.App.Hubs;

public class RoutingHub :
    RoutingHubBase<RoutingHub>,
    IOnionParsingHub,
    IOnionRoutingHub
{
    private readonly SessionManager sessionManager;

    private readonly IEphemeralCollection<OnionQueueItem> onionQueue;

    public RoutingHub(SessionManager sessionManager, IEphemeralCollection<OnionQueueItem> onionQueue)
    {
        this.sessionManager = sessionManager;
        this.onionQueue = onionQueue;
    }

    public string? Address { get; set; }

    public string? DestinationConnectionId { get; set; }

    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    public async Task GenerateToken()
    {
        var token = sessionManager.AddPending(Context.ConnectionId);
        await RespondAsync(nameof(GenerateToken), token);
    }

    public async Task Authenticate(string publicKey, string signature)
    {
        var authenticated = sessionManager.Authenticate(Context.ConnectionId, publicKey, signature);
        await RespondAsync(nameof(Authenticate), authenticated);
    }

    public async Task Synchronize()
    {
        if(sessionManager.TryGetAddress(Context.ConnectionId, out string? address))
        {
            var onions = onionQueue.Where(item => item.Destination == address)
            .Select(item => Convert.ToBase64String(item.Content))
            .ToList();

            await RespondAsync(nameof(Synchronize), onions);
        }
    }

    [OnionParsing]
    [OnionRouting]
    public async Task RouteMessage(string data)
    {
        if (DestinationConnectionId != null && Content != null)
        {
            await SendAsync(DestinationConnectionId, Content);
        }
        else if (Content != null)
        {
            onionQueue.Add(new OnionQueueItem
            {
                Content = Content,
                Destination = Next!
            });
        }
    }
}
