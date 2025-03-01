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

using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Hubs.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Adapters;

public class OnionRoutingHubAdapter : IOnionRoutingHub
{
    private readonly IOnionRoutingHub? onionRouterHub;

    public OnionRoutingHubAdapter(Hub hub)
    {
        onionRouterHub = hub.As<IOnionRoutingHub>();
    }

    public string? DestinationConnectionId
    {
        get => onionRouterHub?.DestinationConnectionId;
        set
        {
            if(onionRouterHub != null)
            {
                onionRouterHub.DestinationConnectionId = value;
            }
        }
    }
}
