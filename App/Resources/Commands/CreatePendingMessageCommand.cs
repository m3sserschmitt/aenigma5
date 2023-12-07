using Enigma5.App.Data;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CreatePendingMessageCommand : IRequest<PendingMessage>
{
    public CreatePendingMessageCommand(string destination, string content)
    {
        Destination = destination;
        Content = content;
    }

    public string Destination { get; private set; }

    public string Content { get; private set; }
}
