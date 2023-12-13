using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class MarkMessagesAsDeliveredHandler
: IRequestHandler<MarkMessagesAsDeliveredCommand>
{
    private readonly EnigmaDbContext _context;

    public MarkMessagesAsDeliveredHandler(EnigmaDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MarkMessagesAsDeliveredCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var messages = _context.Messages.Where(item => item.Destination == command.Destination);

            foreach (var message in messages)
            {
                message.Sent = true;
            }

            _context.UpdateRange(messages);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            // TODO: Log exception!!
        }
    }
}
