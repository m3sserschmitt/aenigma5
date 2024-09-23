using MediatR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Resources.Handlers;

public class RequestResponseLoggingBehavior<TRequest, TResponse>(ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly ILogger<RequestResponseLoggingBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling command {CommandName}: {@Command}", typeof(TRequest).Name, request);
            var response = await next();
            _logger.LogDebug("Command {CommandName} successfully completed with the following response: {@Response}", typeof(TRequest).Name, response);

            return response;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while handling command {CommandName}: {@Command}", typeof(TRequest).Name, request);
#pragma warning disable CS8603 // Possible null reference return.
            return default;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}
