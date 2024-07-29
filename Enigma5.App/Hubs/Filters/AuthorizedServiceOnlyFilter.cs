using Enigma5.App.Attributes;
using Enigma5.App.Common.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;

#if !DEBUG
using Enigma5.App.Hubs.Sessions;
using Enigma5.App.Resources.Queries;
using MediatR;
#endif

namespace Enigma5.App.Hubs.Filters;

public class AuthorizedServiceOnlyFilter
#if !DEBUG
(SessionManager sessionManager, IMediator commandRouter)
#endif
: BaseFilter<IHub, AuthorizedServiceOnlyAttribute>
{
#if !DEBUG
    private readonly SessionManager _sessionManager = sessionManager;

    private readonly IMediator _commandRouter = commandRouter;
#endif

    protected override bool CheckArguments(HubInvocationContext invocationContext) => true;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
#if DEBUG
    => await next(invocationContext);
#else
    => !_sessionManager.TryGetAddress(invocationContext.Context.ConnectionId, out string? address)
    || !await _commandRouter.Send(new CheckAuthorizedServiceQuery(address!))
    ? Task.CompletedTask
    : await next(invocationContext);
#endif
}
