using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CleanupSharedDataCommand(TimeSpan timeSpan): IRequest
{
    public TimeSpan TimeSpan { get; set; } = timeSpan;
}
