﻿using Enigma5.App.Data;
using Enigma5.App.Resources.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class RemoveSharedDataHandler(EnigmaDbContext context) : IRequestHandler<RemoveSharedDataCommand>
{
    private readonly EnigmaDbContext _context = context;

    public async Task Handle(RemoveSharedDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var sharedData = await _context.SharedData.SingleOrDefaultAsync(
                item => item.Tag == request.Tag,
                cancellationToken: cancellationToken);
            
            if (sharedData is not null)
            {
                _context.Remove(sharedData);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // TODO: do something with this exception
        }
    }
}
