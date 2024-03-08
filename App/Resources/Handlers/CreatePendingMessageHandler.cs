using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreatePendingMessageHandler(EnigmaDbContext context) : IRequestHandler<CreatePendingMessageCommand, PendingMessage?>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<PendingMessage?> Handle(CreatePendingMessageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var pendingMessage = new PendingMessage(command.Destination, command.Content, DateTime.UtcNow, false);

            await _context.AddAsync(pendingMessage, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return pendingMessage;
        }
        catch
        {
            // TODO: Log exception!!
            return null;
        }
    }
}
