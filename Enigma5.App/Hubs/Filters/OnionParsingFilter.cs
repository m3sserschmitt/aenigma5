using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Attributes;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Adapters;
using Enigma5.App.Hubs.Sessions;
using Enigma5.Structures;
using Enigma5.App.Models;
using Microsoft.Extensions.Logging;
using Enigma5.App.Models.HubInvocation;

namespace Enigma5.App.Hubs.Filters;

public class OnionParsingFilter(SessionManager sessionManager, ILogger<OnionParsingFilter> logger)
: BaseFilter<IOnionParsingHub, OnionParsingAttribute>
{
    private readonly SessionManager _sessionManager = sessionManager;

    private readonly ILogger<OnionParsingFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext)
     => invocationContext.HubMethodArguments.Count == 1 && invocationContext.HubMethodArguments[0] is RoutingRequest;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var request = invocationContext.MethodInvocationArgument<RoutingRequest>(0);
        if (request != null)
        {
            var decodedData = Convert.FromBase64String(request.Payload!);

            if (decodedData is not null && _sessionManager.TryGetParser(invocationContext.Context.ConnectionId, out var onionParser) &&
            onionParser!.Parse(new Onion { Content = decodedData }))
            {
                _ = new OnionParsingHubAdapter(invocationContext.Hub)
                {
                    Content = onionParser.Content,
                    Next = onionParser.NextAddress
                };

                onionParser.Reset();
                _logger.LogDebug($"Onion from connectionId {{{nameof(invocationContext.Context.ConnectionId)}}} successfully parsed.", invocationContext.Context.ConnectionId);
                return await next(invocationContext);
            }

            _logger.LogDebug($"Could not parse onion from connectionId {{{nameof(invocationContext.Context.ConnectionId)}}}", invocationContext.Context.ConnectionId);
            return EmptyErrorResult.Create(InvocationErrors.ONION_PARSING_FAILED);
        }

        _logger.LogDebug($"Invalid input data for {{{nameof(invocationContext.HubMethodName)}}} method: {{@{nameof(invocationContext.HubMethodArguments)}}}.", invocationContext.HubMethodName, invocationContext.HubMethodArguments);
        return EmptyErrorResult.Create(InvocationErrors.INVALID_INVOCATION_DATA);
    }
}
