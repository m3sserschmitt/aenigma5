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

using Enigma5.App.Common;
using Enigma5.App.Data;
using Enigma5.App.Hangfire;
using Enigma5.App.Resources.Commands;
using Enigma5.App.UI;
using Enigma5.Security.Contracts;
using Hangfire;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class SetMasterPassphraseHandler(
    ICertificateManager certificateManager,
    NetworkGraph networkGraph,
    DashboardUIState dashboardUIState)
: IRequestHandler<SetMasterPassphraseCommand, CommandResult<bool>>
{
    private readonly ICertificateManager _certificateManager = certificateManager;

    private readonly NetworkGraph _networkGraph = networkGraph;

    private readonly DashboardUIState _dashboardUIState = dashboardUIState;

    public async Task<CommandResult<bool>> Handle(SetMasterPassphraseCommand request, CancellationToken cancellationToken)
    {
        var result = await _certificateManager.SetupAsync(request.Passphrase) && await _networkGraph.GenerateLocalVertexAsync(cancellationToken);
        if (result)
        {
            SendInvokeNetworkBridgeCommand();
        }
        UpdateDashboardUIState();
        return CommandResult.CreateResultSuccess(result);
    }

    private static void SendInvokeNetworkBridgeCommand()
    {
        RecurringJob.AddOrUpdate<MediatorHangfireBridge>(
            Constants.InvokeNetworkBridgeRecurringJob,
            bridge => bridge.Send(new InvokeNetworkBridgeCommand()),
            Constants.InvokeNetworkBridgeJobInterval
        );
        BackgroundJob.Enqueue<MediatorHangfireBridge>(bridge => bridge.Send(new InvokeNetworkBridgeCommand()));
    }

    private void UpdateDashboardUIState()
    {
        _dashboardUIState.PrivateKeyUnlocked = !string.IsNullOrWhiteSpace(_networkGraph.LocalVertex?.SignedData);
    }
}
