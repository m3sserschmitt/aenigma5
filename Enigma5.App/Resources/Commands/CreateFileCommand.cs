using Enigma5.App.Models;
using Enigma5.App.Resources.Handlers;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CreateFileCommand(IFormFile file, int maxAccessCount)
: IRequest<CommandResult<SharedDataDto>>
{
    public IFormFile File { get; private set; } = file;

    public int MaxAccessCount { get; private set; } = maxAccessCount;
}
