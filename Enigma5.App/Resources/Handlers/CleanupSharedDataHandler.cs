using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CleanupSharedDataHandler(EnigmaDbContext context)
: IRequestHandler<CleanupSharedDataCommand>
{
    private readonly EnigmaDbContext _context = context;

    public async Task Handle(CleanupSharedDataCommand request, CancellationToken cancellationToken)
    {
        var time = DateTimeOffset.Now - request.TimeSpan;
        var sharedData = _context.SharedData.Where(item => time > item.DateCreated);
        _context.RemoveRange(sharedData);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
