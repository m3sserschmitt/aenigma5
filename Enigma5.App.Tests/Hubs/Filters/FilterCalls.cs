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

using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.Tests.Base;
using Microsoft.AspNetCore.SignalR;

namespace Enigma5.App.Tests.Hubs.Filters;

internal static class FilterCalls
{
    public const string Passed = "the hub method ran";

    // Runs a filter around a hub call. If the filter lets the call through, the result is Passed.
    public static ValueTask<object?> Run(IHubFilter filter, HubInvocationContext call)
    => filter.InvokeMethodAsync(call, _ => ValueTask.FromResult<object?>(Passed));

    public static string SingleError(object? result)
    => Assert.Single(Assert.IsAssignableFrom<InvocationResultDto<object>>(result).Errors).Message!;
}
