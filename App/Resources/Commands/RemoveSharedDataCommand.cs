using MediatR;

namespace Enigma5.App.Resources.Commands;

public class RemoveSharedDataCommand(string tag): IRequest
{
    public string Tag { get; set; } = tag;
}
