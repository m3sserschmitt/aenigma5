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
using Enigma5.App.Hubs;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Models.HubInvocation;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Enigma5.App.Tests.Hubs.Filters;

[ExcludeFromCodeCoverage]
public class LogFilterTests : FiltersTestBase<LogFilter>
{
    [Fact]
    public async Task ShouldReturnSuccessResult()
    {
        // Arrange
        var returnValue = new SuccessResult<string>("Success");
        var valueTask = ValueTask.FromResult(returnValue);
        _next(_hubInvocationContext).Returns(returnValue);
        
        // Act
        var result = await _filter.InvokeMethodAsync(_hubInvocationContext, _next);

        // Assert
        var response = result as SuccessResult<string>;
        response.Should().NotBeNull();
        response!.Data.Should().Be(returnValue.Data);       
        await _next.Received(1)(_hubInvocationContext);
    }

    [Fact]
    public async Task ShouldReturnErrorWhenExceptionThrown()
    {
        // Arrange
        _next(_hubInvocationContext).Throws(new Exception());

        // Act
        var result = await _filter.InvokeMethodAsync(_hubInvocationContext, _next);
        var response = result as EmptyErrorResult;
        response.Should().NotBeNull();
        response!.Errors.Should().HaveCount(1);
        response.Errors.Single().Message.Should().Be(InvocationErrors.INTERNAL_ERROR);
        await _next.Received(1)(_hubInvocationContext);
    }

    [Fact]
    public async Task ShouldReturnErrorResult()
    {
        // Arrange
        var returnValue = new ErrorResult<string>("Failure", [ new("Error message") ]);
        var valueTask = ValueTask.FromResult(returnValue);
        _next(_hubInvocationContext).Returns(returnValue);
        
        // Act
        var result = await _filter.InvokeMethodAsync(_hubInvocationContext, _next);

        // Assert
        var response = result as ErrorResult<string>;
        response.Should().NotBeNull();
        response!.Data.Should().Be(returnValue.Data);
        response.Errors.Should().HaveCount(1);
        response.Errors.Single().Message.Should().Be("Error message");
        await _next.Received(1)(_hubInvocationContext);
    }
}
