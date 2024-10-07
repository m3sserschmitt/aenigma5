using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class CleanupMessagesHandler(EnigmaDbContext context)
: IRequestHandler<CleanupMessagesCommand>
{
    private readonly EnigmaDbContext _context = context;

    public async Task Handle(CleanupMessagesCommand command, CancellationToken cancellationToken)
    {
        // TODO: refactor this query
        var time = DateTimeOffset.Now - command.TimeSpan;
        var messages = await _context.Messages.ToListAsync(cancellationToken: cancellationToken);
        _context.Messages.RemoveRange(messages.Where(item =>
            time > item.DateReceived ||
            (command.RemoveDelivered && item.Sent)));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
