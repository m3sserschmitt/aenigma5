using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Adapters;
using Enigma5.App.Hubs.Sessions;
using Enigma5.Structures;
using Enigma5.App.Models;

namespace Enigma5.App.Hubs.Filters;

public class OnionParsingFilter(SessionManager sessionManager)
: BaseFilter<IOnionParsingHub, OnionParsingAttribute>
{
    private readonly SessionManager _sessionManager = sessionManager;

    protected override bool CheckArguments(HubInvocationContext invocationContext)
     => invocationContext.HubMethodArguments.Count == 1 && invocationContext.HubMethodArguments[0] is RoutingRequest;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var request = invocationContext.MethodInvocationArgument<RoutingRequest>(0);
        if (request != null)
        {
            var decodedData = Convert.FromBase64String(request.Payload!);

            if (_sessionManager.TryGetParser(invocationContext.Context.ConnectionId, out var onionParser) &&
            onionParser!.Parse(new Onion { Content = decodedData }))
            {
                _ = new OnionParsingHubAdapter(invocationContext.Hub)
                {
                    Content = onionParser.Content,
                    Next = onionParser.NextAddress
                };

                onionParser.Reset();
            }
        }

        return await next(invocationContext);
    }
}
