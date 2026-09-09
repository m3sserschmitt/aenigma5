/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using Enigma5.App.Models.Contracts.Hubs;
using Enigma5.App.Models.HubInvocation;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Filters;

public class LogFilter(ILogger<LogFilter> logger) : IHubFilter
{
    private readonly ILogger<LogFilter> _logger = logger;

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        _logger.LogDebug(
            $"Invoking {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{nameof(Common.Constants.Serilog.ConnectionIdKey)}}} with the following data: {{@{Common.Constants.Serilog.HubMethodArgumentsKey}}}.",
            invocationContext.HubMethodName,
            invocationContext.Context.ConnectionId,
            invocationContext.HubMethodArguments
        );

        object? result = null;
        try
        {
            result = await next(invocationContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                $"Exception encountered while invoking {{{Common.Constants.Serilog.HubMethodNameKey}}} method on connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} with the following data: {{@{Common.Constants.Serilog.HubMethodArgumentsKey}}}.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId,
                invocationContext.HubMethodArguments
                );
        }

        if (result is null)
        {
            _logger.LogError(
                $"Invocation of {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} completed with null result having the following data: {{@{Common.Constants.Serilog.HubMethodArgumentsKey}}}.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId,
                invocationContext.HubMethodArguments
                );
            return ErrorResultDto.Create(InvocationErrors.INTERNAL_ERROR);
        }

        if (result is IInvocationResult invocationResult)
        {
            if (!invocationResult.Success)
            {
                _logger.LogDebug(
                    $"Invocation of {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} completed with no success with the following errors {{@{Common.Constants.Serilog.HubMethodInvocationErrorsKey}}}.",
                    invocationContext.HubMethodName,
                    invocationContext.Context.ConnectionId,
                    invocationResult.Errors
                    );
            }
            else
            {
                _logger.LogDebug(
                    $"Invocation of {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} completed successfully.",
                    invocationContext.HubMethodName,
                    invocationContext.Context.ConnectionId
                    );
            }
        }
        else
        {
            _logger.LogError(
                $"Invocation of {{{Common.Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Common.Constants.Serilog.ConnectionIdKey}}} completed with unexpected result.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId
                );
        }

        return result;
    }
}
