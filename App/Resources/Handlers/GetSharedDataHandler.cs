using Enigma5.App.Data;
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class GetSharedDataHandler(EnigmaDbContext context) : IRequestHandler<GetSharedDataQuery, SharedData?>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<SharedData?> Handle(GetSharedDataQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.SharedData.SingleOrDefaultAsync(
                item => item.Tag == request.Tag,
                cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
