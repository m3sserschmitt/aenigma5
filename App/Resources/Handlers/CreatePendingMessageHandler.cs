using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreatePendingMessageHandler : IRequestHandler<CreatePendingMessageCommand, PendingMessage>
{
    private readonly EnigmaDbContext _context;

    public CreatePendingMessageHandler(EnigmaDbContext context) 
    {
        _context = context;
    }

    public async Task<PendingMessage> Handle(CreatePendingMessageCommand command, CancellationToken cancellationToken)
    {
        var pendingMessage = new PendingMessage
        {
            Destination = command.Destination,
            Content = command.Content,
            DateReceived = DateTime.Now,
            Sent = false
        };

        await _context.AddAsync(pendingMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return pendingMessage;
    }
}
