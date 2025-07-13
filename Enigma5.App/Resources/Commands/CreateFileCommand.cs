using Enigma5.App.Models;
using Enigma5.App.Resources.Handlers;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Enigma5.App.Resources.Commands;

public class CreateFileCommand(IFormFile file, int maxAccessCount)
: IRequest<CommandResult<SharedData>>
{
    public IFormFile File { get; private set; } = file;

    public int MaxAccessCount { get; private set; } = maxAccessCount;
}
