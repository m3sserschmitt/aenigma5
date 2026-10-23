/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using Enigma5.App.Attributes;
using Enigma5.App.Common;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;

namespace Enigma5.App.Hubs;

// The second generation of hub methods, which replace methods of the same name without the number.
// The earlier methods are in RoutingHub.cs, and what both share is in RoutingHubHelpers.cs.
public partial class RoutingHub
{
    [Authenticated]
    [ValidateModel]
    [BlacklistAuthorization]
    public async Task<InvocationResultDto<List<PendingMessageDto>>> Pull2(PullRequestDto request)
    {
        if (ClientAddress is null)
        {
            _logger.LogError($"ClientAddress null while invoking {{{Constants.Serilog.HubMethodNameKey}}} for connectionId {{{Constants.Serilog.ConnectionIdKey}}}.",
            nameof(Pull2),
            Context.ConnectionId);
            return Error<List<PendingMessageDto>>(InvocationErrors.INTERNAL_ERROR);
        }

        return Ok(await GetPendingMessagesAsync(ClientAddress, request.InfId, Constants.MessagesPageSize));
    }

    [Authenticated]
    [ValidateModel]
    [BlacklistAuthorization]
    public Task<InvocationResultDto<bool>> Cleanup2(CleanupRequestDto request)
    => ConfirmDeliveryAsync(nameof(Cleanup2), request.SupId);
}
