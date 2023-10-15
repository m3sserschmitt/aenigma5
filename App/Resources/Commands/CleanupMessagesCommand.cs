using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CleanupMessagesCommand : IRequest
{
    public TimeSpan TimeSpan { get; set; }

    public bool RemoveDelivered { get; set; }
}
