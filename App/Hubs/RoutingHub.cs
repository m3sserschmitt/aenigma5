using Enigma5.App.Hubs.Contracts;
using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Resources.Commands;
using MediatR;
using Enigma5.App.Resources.Queries;
using System.Text.Json;

namespace Enigma5.App.Hubs;

public class RoutingHub :
    RoutingHubBase<RoutingHub>,
    IOnionParsingHub,
    IOnionRoutingHub
{
    private readonly SessionManager _sessionManager;

    private readonly IMediator _commandRouter;

    public RoutingHub(SessionManager sessionManager, IMediator commandRouter)
    {
        _sessionManager = sessionManager;
        _commandRouter = commandRouter;
    }

    public string? Address { get; set; }

    public string? DestinationConnectionId { get; set; }

    public int Size { get; set; }

    public string? Next { get; set; }

    public byte[]? Content { get; set; }

    public async Task GenerateToken()
    {
        var token = _sessionManager.AddPending(Context.ConnectionId);
        await RespondAsync(nameof(GenerateToken), token);
    }

    private async Task Synchronize()
    {
        if (_sessionManager.TryGetAddress(Context.ConnectionId, out string? address))
        {
            var query = new GetPendingMessagesByDestinationQuery
            {
                Destination = address!
            };

            var onions = await _commandRouter.Send(query);

            if (onions.Any())
            {
                await RespondAsync("Synchronize", onions.Select(item => JsonSerializer.Serialize(new
                {
                    item.Content,
                    item.DateReceived
                })));

                var command = new MarkMessagesAsDeliveredCommand
                {
                    Destination = address!
                };

                await _commandRouter.Send(command);
            }
        }
    }

    public async Task Authenticate(string publicKey, string signature)
    {
        var authenticated = _sessionManager.Authenticate(Context.ConnectionId, publicKey, signature);
        await RespondAsync(nameof(Authenticate), authenticated);
        await Synchronize();
    }

    [OnionParsing]
    [OnionRouting]
    public async Task RouteMessage(string data)
    {
        if (DestinationConnectionId != null && Content != null)
        {
            await RouteMessage(DestinationConnectionId, Content);
        }
        else if (Content != null)
        {
            var createPendingMessageCommand = new CreatePendingMessageCommand
            {
                Content = Convert.ToBase64String(Content),
                Destination = Next!
            };

            await _commandRouter.Send(createPendingMessageCommand);
        }
    }
}
