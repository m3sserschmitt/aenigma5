using Enigma5.App.Data;
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class GetPendingMessagesByDestinationHandler(EnigmaDbContext context)
: IRequestHandler<GetPendingMessagesByDestinationQuery, IEnumerable<PendingMessage>>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<IEnumerable<PendingMessage>> Handle(GetPendingMessagesByDestinationQuery query, CancellationToken cancellationToken)
    => await _context.Messages.Where(item => item.Destination == query.Destination && item.Sent == false).ToListAsync(cancellationToken: cancellationToken);
}
