using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class CleanupSharedDataHandler(EnigmaDbContext context)
: IRequestHandler<CleanupSharedDataCommand>
{
    private readonly EnigmaDbContext _context = context;

    public async Task Handle(CleanupSharedDataCommand request, CancellationToken cancellationToken)
    {
        // TODO: refactor this query
        var time = DateTimeOffset.Now - request.TimeSpan;
        var sharedData = await _context.SharedData.ToListAsync(cancellationToken: cancellationToken);
        _context.RemoveRange(sharedData.Where(item => time > item.DateCreated));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
