/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

    This file is part of Aenigma project.

    Aenigma is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Aenigma is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Aenigma.  If not, see <https://www.gnu.org/licenses/>.
*/

using Enigma5.App.Data;
using Enigma5.App.Resources.Queries;
using Enigma5.Crypto.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class CheckAuthorizedServiceHandler(EnigmaDbContext context)
: IRequestHandler<CheckAuthorizedServiceQuery, CommandResult<bool>>
{
    private readonly EnigmaDbContext _context = context;

    public async Task<CommandResult<bool>> Handle(CheckAuthorizedServiceQuery request, CancellationToken cancellationToken = default)
    => !request.Address.IsValidAddress() ?
    CommandResult.CreateResultFailure<bool>() :
    CommandResult.CreateResultSuccess(await _context.AuthorizedServices.AnyAsync(item => item.Address == request.Address, cancellationToken));
}
