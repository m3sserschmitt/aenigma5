using Enigma5.App.Data;
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class GetPendingMessagesByDestinationHandler
: IRequestHandler<GetPendingMessagesByDestinationQuery, IEnumerable<PendingMessage>>
{
    private readonly EnigmaDbContext _context;

    public GetPendingMessagesByDestinationHandler(EnigmaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PendingMessage>> Handle(GetPendingMessagesByDestinationQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Messages.Where(item => item.Destination == query.Destination && item.Sent == false).ToListAsync(cancellationToken: cancellationToken);
        }
        catch
        {
            // TODO: Log exception!!
            return new List<PendingMessage>();
        }
    }
}