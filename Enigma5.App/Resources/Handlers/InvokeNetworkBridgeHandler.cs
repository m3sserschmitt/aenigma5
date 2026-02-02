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

using Enigma5.App.NetworkBridge;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Queries;
using Enigma5.App.UI;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class InvokeNetworkBridgeHandler(IMediator mediator, Bridge bridge, DashboardUIState dashboardUIState)
: IRequestHandler<InvokeNetworkBridgeCommand, CommandResult<bool>>
{
    private readonly IMediator _mediator = mediator;

    private readonly Bridge _bridge = bridge;

    private readonly DashboardUIState _dashboardUIState = dashboardUIState;

    public async Task<CommandResult<bool>> Handle(InvokeNetworkBridgeCommand request, CancellationToken cancellationToken)
    {
        await UpdateDashboardUIState(cancellationToken);
        return CommandResult.CreateResultSuccess(await _bridge.StartAsync());
    }

    private async Task UpdateDashboardUIState(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPeersQuery(), cancellationToken);
        if (result.IsSuccessNotNullResultValue())
        {
            await _dashboardUIState.SetOutboundPeersAsync([.. result.Value!]);
        }
    }
}
