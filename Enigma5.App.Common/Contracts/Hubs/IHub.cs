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

using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;

namespace Enigma5.App.Common.Contracts.Hubs;

public interface IEnigmaHub
{
    Task<InvocationResult<string>> GenerateToken();

    Task<InvocationResult<List<PendingMessage>>> Pull();

    Task<InvocationResult<bool>> Cleanup();

    Task<InvocationResult<bool>> Authenticate(AuthenticationRequest request);

    Task<InvocationResult<Signature>> SignToken(SignatureRequest request);

    Task<InvocationResult<bool>> Broadcast(VertexBroadcastRequest request);

    Task<InvocationResult<bool>> TriggerBroadcast(TriggerBroadcastRequest request);

    Task<InvocationResult<bool>> RouteMessage(RoutingRequest request);

    Task<InvocationResult<bool>> RouteMessages(RoutingRequestBulk request);
}
