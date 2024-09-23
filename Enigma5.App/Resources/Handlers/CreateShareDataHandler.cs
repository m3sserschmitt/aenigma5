using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreateShareDataHandler(EnigmaDbContext context) : IRequestHandler<CreateShareDataCommand, string?>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<string?> Handle(CreateShareDataCommand request, CancellationToken cancellationToken)
    {
        var sharedData = new SharedData(request.SignedData, request.AccessCount);
        await _context.AddAsync(sharedData, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return sharedData.Tag;
    }
}
