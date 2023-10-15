using MediatR;

namespace Enigma5.App.Resources.Commands;

public class MarkMessagesAsDeliveredCommand : IRequest
{
    public string Destination { get; set; } = string.Empty;
}
