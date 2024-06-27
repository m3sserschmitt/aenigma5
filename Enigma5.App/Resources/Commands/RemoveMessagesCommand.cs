using MediatR;

namespace Enigma5.App.Resources.Commands;

public class RemoveMessagesCommand(string destination) : IRequest
{
    public string Destination { get; private set; } = destination;
}
