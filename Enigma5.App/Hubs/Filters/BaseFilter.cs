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

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Hubs.Filters;

[ExcludeFromCodeCoverage]
public abstract class BaseFilter<THub, TMarker> : IHubFilter
where THub : class
where TMarker : Attribute
{
    protected virtual bool CheckMarker(HubInvocationContext invocationContext) =>
    Attribute.GetCustomAttribute(invocationContext.HubMethod, typeof(TMarker)) != null;

    protected abstract bool CheckArguments(HubInvocationContext invocationContext);

    protected virtual bool CheckHubType(HubInvocationContext invocationContext) => invocationContext.Hub is THub;

    public abstract ValueTask<object?> Handle(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next);

    protected bool Check(HubInvocationContext invocationContext) =>
    CheckMarker(invocationContext) && CheckHubType(invocationContext) && CheckArguments(invocationContext);
    
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next) =>
    Check(invocationContext) ? await Handle(invocationContext, next) : await next(invocationContext);
}
