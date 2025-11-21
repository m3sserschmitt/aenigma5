/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Enigma5.App.Resources.Commands;
using Enigma5.Security.Contracts;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class SetMasterPassphraseHandler(ICertificateManager certificateManager, NetworkGraph networkGraph)
: IRequestHandler<SetMasterPassphraseCommand, CommandResult<bool>>
{
    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly NetworkGraph _networkGraph = networkGraph;

    public Task<CommandResult<bool>> Handle(SetMasterPassphraseCommand request, CancellationToken cancellationToken)
    => Task.FromResult(CommandResult.CreateResultSuccess(_certificateManager.Setup(request.Passphrase) && _networkGraph.CreateInitialVertex()));
}
