using Enigma5.App.Attributes;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Extensions;
using Enigma5.App.Models.Contracts;
using Enigma5.App.Models.HubInvocation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Hubs.Filters;

public class ValidateModelFilter(ILogger<ValidateModelFilter> logger) : BaseFilter<IHub, ValidateModelAttribute>
{
    private readonly ILogger<ValidateModelFilter> _logger = logger;

    protected override bool CheckArguments(HubInvocationContext invocationContext)
    => invocationContext.HubMethodArguments.Count == 1 && invocationContext.HubMethodArguments[0] is IValidatable;

    protected override async ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // TODO: refactor this to support any number of arguments;
        var data = invocationContext.MethodInvocationArgument<IValidatable>(0);

        if (data is null)
        {
            _logger.LogDebug(
                $"Invalid input data for {{{nameof(invocationContext.HubMethodName)}}} invocation on connectionId {{{nameof(invocationContext.Context.ConnectionId)}}}; arguments list: {{@{nameof(invocationContext.HubMethodArguments)}}}.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId,
                invocationContext.HubMethodArguments
                );
            return EmptyErrorResult.Create(InvocationErrors.INVALID_INVOCATION_DATA);
        }

        var errors = data.Validate().ToList();

        if (errors.Count != 0)
        {
            _logger.LogDebug(
                $"Invalid input data for {{{nameof(invocationContext.HubMethodName)}}} invocation on connectionId {{{nameof(invocationContext.Context.ConnectionId)}}}; arguments list: {{@{nameof(invocationContext.HubMethodArguments)}}}.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId,
                invocationContext.HubMethodArguments
                );
            return new EmptyErrorResult(errors);
        }

        _logger.LogDebug(
            $"Request model successfully validated for {{{nameof(invocationContext.HubMethodName)}}} invocation on connectionId {{{nameof(invocationContext.Context.ConnectionId)}}}.",
            invocationContext.HubMethodName,
            invocationContext.Context.ConnectionId
            );
        return await next(invocationContext);
    }
}
