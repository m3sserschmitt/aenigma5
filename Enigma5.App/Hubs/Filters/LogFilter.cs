using Enigma5.App.Models.HubInvocation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Hubs.Filters;

public class LogFilter(ILogger<LogFilter> logger) : IHubFilter
{
    private readonly ILogger<LogFilter> _logger = logger;

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        _logger.LogDebug(
            $"Invoking {{{nameof(invocationContext.HubMethodName)}}} for connectionId {{{nameof(invocationContext.Context.ConnectionId)}}} with the following data: {{@{nameof(invocationContext.HubMethodArguments)}}}.",
            invocationContext.HubMethodName,
            invocationContext.Context.ConnectionId,
            invocationContext.HubMethodArguments
        );

        dynamic? result = null;
        try
        {
            result = await next(invocationContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"Exception encountered while invoking {{{nameof(invocationContext.HubMethodName)}}} method on {{{nameof(invocationContext.Context.ConnectionId)}}} connectionId.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId
                );
        }

        if (result is null)
        {
            _logger.LogError(
                $"Invocation of {{{nameof(invocationContext.HubMethodName)}}} for {{{nameof(invocationContext.Context.ConnectionId)}}} completed with null result.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId
                );
            return EmptyErrorResult.Create(InvocationErrors.INTERNAL_ERROR);
        }

        if (!result.Success)
        {
            _logger.LogDebug(
                $"Invocation of {{{nameof(invocationContext.HubMethodName)}}} for {{{nameof(invocationContext.Context.ConnectionId)}}} completed with no success.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId
                );
        }
        else
        {
            _logger.LogDebug(
                $"Invocation of {{{nameof(invocationContext.HubMethodName)}}} for {{{nameof(invocationContext.Context.ConnectionId)}}} completed successfully.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId
                );
        }

        return result;
    }
}
