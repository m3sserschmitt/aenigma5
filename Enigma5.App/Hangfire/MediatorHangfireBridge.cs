using MediatR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Hangfire;

public class MediatorHangfireBridge(
    IMediator mediator,
    ILogger<MediatorHangfireBridge> logger
    )
{
    private readonly IMediator _mediator = mediator;

    private readonly ILogger<MediatorHangfireBridge> _logger = logger;

    public Task Send(IRequest command)
    {
        _logger.LogInformation("Executing {CommandName} for Hangfire Job: {@Command}", command.GetType().Name, command);
        return _mediator.Send(command);
    }
}
