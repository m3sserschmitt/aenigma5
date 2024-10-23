using Enigma5.App.Models;
using Enigma5.App.Resources.Handlers;
using MediatR;

namespace Enigma5.App.Resources.Commands;

public class CreateSharedDataCommand(SharedDataCreate sharedDataCreate) : IRequest<CommandResult<SharedData>>
{
    public SharedDataCreate SharedDataCreate { get; private set; } = sharedDataCreate;
}
