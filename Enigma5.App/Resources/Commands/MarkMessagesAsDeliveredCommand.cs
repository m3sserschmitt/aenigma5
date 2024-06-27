using MediatR;

namespace Enigma5.App.Resources.Commands;

public class MarkMessagesAsDeliveredCommand : IRequest
{
    public MarkMessagesAsDeliveredCommand(string destination)
    {
        Destination = destination;
    }
    
    public string Destination { get; private set; }
}
