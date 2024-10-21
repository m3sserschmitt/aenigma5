/*
    Aenigma - Federal messaging system
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
