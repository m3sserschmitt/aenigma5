using Enigma5.App.Data;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CreatePendingMessageCommand : IRequest<PendingMessage>
{
    public string Destination { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
