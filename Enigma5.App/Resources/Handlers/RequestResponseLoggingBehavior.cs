/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class RequestResponseLoggingBehavior<TRequest, TResponse>(ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse: new()
{
    private readonly ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug($"Handling command {{@{Common.Constants.Serilog.CommandKey}}}.", request);
            var response = await next();
            _logger.LogDebug($"Command {{@{Common.Constants.Serilog.CommandKey}}} successfully completed with the following response: {{@{Common.Constants.Serilog.CommandResultKey}}}.", request, response);

            return response;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, $"Exception occurred while handling command {{@{Common.Constants.Serilog.CommandKey}}}.", request);
            return new TResponse();
        }
    }
}
