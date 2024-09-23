using Enigma5.App.Attributes;
using Enigma5.App.Common.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Enigma5.App.Models.HubInvocation;

namespace Enigma5.App.Hubs.Filters;

public class AuthorizedServiceOnlyFilter(
    SessionManager sessionManager,
    IMediator commandRouter,
    ILogger<AuthorizedServiceOnlyFilter> logger
    ) : BaseFilter<IHub, AuthorizedServiceOnlyAttribute>
{
    private readonly SessionManager _sessionManager = sessionManager;

    private readonly IMediator _commandRouter = commandRouter;

    private readonly ILogger<AuthorizedServiceOnlyFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext) => true;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
#if DEBUG
        _logger.LogDebug($"Authorization skipped for {{{nameof(invocationContext.HubMethodName)}}} invocation in debug mode.", invocationContext.HubMethodName);
        return await next(invocationContext);
#endif
#pragma warning disable CS0162 // Unreachable code detected
        if (!_sessionManager.TryGetAddress(invocationContext.Context.ConnectionId, out string? address))
        {
            _logger.LogDebug($"Connection {{{nameof(invocationContext.Context.ConnectionId)}}} not authenticated for {{{nameof(invocationContext.HubMethodName)}}} invocation.", invocationContext.Context.ConnectionId, invocationContext.HubMethodName);
            return EmptyErrorResult.Create(InvocationErrors.NOT_AUTHORIZED);
        }
#pragma warning restore CS0162 // Unreachable code detected

        if (!await _commandRouter.Send(new CheckAuthorizedServiceQuery(address!)))
        {
            _logger.LogDebug($"Connection {{{nameof(invocationContext.Context.ConnectionId)}}} not authorized for {{{nameof(invocationContext.HubMethodName)}}} invocation.", invocationContext.Context.ConnectionId, invocationContext.HubMethodName);
            return EmptyErrorResult.Create(InvocationErrors.AUTHENTICATION_REQUIRED);
        }

        _logger.LogDebug($"{{{nameof(invocationContext.HubMethodName)}}} invocation authorized for {{{nameof(invocationContext.Context.ConnectionId)}}}.", invocationContext.HubMethodName, invocationContext.Context.ConnectionId);
        return await next(invocationContext);
    }
}
