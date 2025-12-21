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
    Task<InvocationResultDto<string>> GenerateToken();

    Task<InvocationResultDto<List<PendingMessageDto>>> Pull();

    Task<InvocationResultDto<bool>> Cleanup();

    Task<InvocationResultDto<bool>> Authenticate(AuthenticationRequestDto request);

    Task<InvocationResultDto<SignatureDto>> SignToken(SignatureRequestDto request);

    Task<InvocationResultDto<bool>> Broadcast(VertexBroadcastRequestDto request);

    Task<InvocationResultDto<bool>> TriggerBroadcast(TriggerBroadcastRequestDto request);

    Task<InvocationResultDto<bool>> RouteMessage(RoutingRequestDto request);
}
