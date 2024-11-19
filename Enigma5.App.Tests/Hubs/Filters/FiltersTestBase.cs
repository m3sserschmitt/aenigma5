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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Autofac;
using Enigma5.App.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace Enigma5.App.Tests.Hubs.Filters;

[ExcludeFromCodeCoverage]
public class FiltersTestBase<T> : AppTestBase
where T : notnull
{
    protected static readonly string _testMethodName = "test-method";

    protected readonly T _filter;

    protected readonly HubCallerContext _hubCallerContext;

    protected readonly HubInvocationContext _hubInvocationContext;

    protected readonly MethodInfo _methodInfo;

    protected readonly IReadOnlyList<object?> _hubMethodArguments;

    protected readonly Func<HubInvocationContext, ValueTask<object?>> _next;

    public FiltersTestBase()
    {
        _filter = _container.Resolve<T>();
        _hubCallerContext = Substitute.For<HubCallerContext>();
        _methodInfo = Substitute.For<MethodInfo>();
        _hubMethodArguments = Substitute.For<IReadOnlyList<object?>>();
        _hubInvocationContext = new HubInvocationContext(_hubCallerContext, Substitute.For<IServiceProvider>(), _hub, _methodInfo, _hubMethodArguments);
        _methodInfo.Name.Returns(_testMethodName);
        _hubCallerContext.ConnectionId.Returns(_testConnectionId1);
        _next = Substitute.For<Func<HubInvocationContext, ValueTask<object?>>>();
    }
}
