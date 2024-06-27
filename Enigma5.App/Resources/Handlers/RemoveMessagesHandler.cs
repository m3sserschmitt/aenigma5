using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class RemoveMessagesHandler(EnigmaDbContext context)
: IRequestHandler<RemoveMessagesCommand>
{
    private readonly EnigmaDbContext _context = context;

    public async Task Handle(RemoveMessagesCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var messages = _context.Messages.Where(item => item.Destination == command.Destination);

            _context.RemoveRange(messages);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // TODO: Log exception!!
        }
    }
}
