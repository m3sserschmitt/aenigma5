using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreateShareDataHandler(EnigmaDbContext context) : IRequestHandler<CreateShareDataCommand, Guid?>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<Guid?> Handle(CreateShareDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shareData = new ShareData(request.SignedData);

            await _context.AddAsync(shareData, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return shareData.Tag;
        }
        catch (Exception)
        {
            // TODO: do something with the exception
            return null;
        }
    }
}
