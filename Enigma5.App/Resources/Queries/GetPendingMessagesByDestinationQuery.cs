using Enigma5.App.Data;
using MediatR;

namespace Enigma5.App.Resources.Queries;

public class GetPendingMessagesByDestinationQuery(string destination)
: IRequest<IEnumerable<PendingMessage>>
{
    public string Destination { get; set; } = destination;
}
