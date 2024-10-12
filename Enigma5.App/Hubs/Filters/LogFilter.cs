/*
    Aenigma - Onion Routing based messaging application
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

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
