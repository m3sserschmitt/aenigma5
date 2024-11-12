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
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.App.Tests.Helpers;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Enigma5.App.Tests.Hubs.Filters;

[ExcludeFromCodeCoverage]
public class ValidateModelFilterTests : FiltersTestBase<ValidateModelFilter>
{
    [Fact]
    public async Task ShouldValidateModel()
    {
        // Arrange
        var request = DataSeeder.ModelsFactory.CreateSignatureRequest();
        _hubMethodArguments[0].Returns(request);

        // Act
        await _filter.Handle(_hubInvocationContext, _next);

        // Assert
        await _next.Received(1)(_hubInvocationContext);
    }

    [Fact]
    public async Task ShouldNotValidateForNotExistentIValidableObject()
    {
        // Arrange
        _hubMethodArguments[0].Throws(new IndexOutOfRangeException());

        // Act
        var result = await _filter.Handle(_hubInvocationContext, _next);

        // Assert
        var response = result as EmptyErrorResult;
        response.Should().NotBeNull();
        response!.Errors.Should().HaveCount(1);
        response.Errors.Single().Message.Should().Be(InvocationErrors.INVALID_INVOCATION_DATA);
        await _next.DidNotReceiveWithAnyArgs()(_hubInvocationContext);
    }

    [Fact]
    public async Task ShouldReturnErrorsForInvalidRequest()
    {
        // Arrange
        var request = new SignatureRequest { Nonce = null };
        _hubMethodArguments[0].Returns(request);

        // Act
        var result = await _filter.Handle(_hubInvocationContext, _next);

        // Assert
        var response = result as EmptyErrorResult;
        response.Should().NotBeNull();
        response!.Errors.Should().HaveCount(2);
        var error = response.Errors.First();
        error.Message.Should().Be(ValidationErrors.NULL_REQUIRED_PROPERTIES);
        error.Properties.Should().HaveCount(1);
        error.Properties.Should().Contain(nameof(SignatureRequest.Nonce));
        await _next.DidNotReceiveWithAnyArgs()(_hubInvocationContext);
    }
}
