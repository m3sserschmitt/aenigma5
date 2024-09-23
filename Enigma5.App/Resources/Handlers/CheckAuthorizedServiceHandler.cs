using Enigma5.App.Data;
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class CheckAuthorizedServiceHandler(EnigmaDbContext context)
: IRequestHandler<CheckAuthorizedServiceQuery, bool>
{
    private readonly EnigmaDbContext _context = context;
    
    public async Task<bool> Handle(CheckAuthorizedServiceQuery request, CancellationToken cancellationToken)
    => await _context.AuthorizedServices.AnyAsync(item => item.Address == request.Address, cancellationToken);
}
