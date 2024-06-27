using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CleanupMessagesCommand : IRequest
{
    public CleanupMessagesCommand(TimeSpan timeSpan, bool removeDelivered)
    {
        TimeSpan = timeSpan;
        RemoveDelivered = removeDelivered;
    }
    
    public TimeSpan TimeSpan { get; private set; }

    public bool RemoveDelivered { get; private set; }
}
