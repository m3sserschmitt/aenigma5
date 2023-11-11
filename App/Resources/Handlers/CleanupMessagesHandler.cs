using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CleanupMessagesHandler
: IRequestHandler<CleanupMessagesCommand>
{
    private readonly EnigmaDbContext _context;

    public CleanupMessagesHandler(EnigmaDbContext context)
    {
        _context = context;
    }

    public async Task Handle(CleanupMessagesCommand command, CancellationToken cancellationToken)
    {
        var time = DateTime.Now - command.TimeSpan;

        var messages = _context.Messages
        .Where(item =>
            time > item.DateReceived ||
            (command.RemoveDelivered && item.Sent));

        _context.Messages.RemoveRange(messages);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
