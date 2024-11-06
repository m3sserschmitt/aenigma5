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

using Enigma5.App.Tests.Helpers;
using Enigma5.Crypto.DataProviders;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;
using System.Text.RegularExpressions;
using Enigma5.Crypto.Extensions;
using Enigma5.App.Common.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Enigma5.App.Tests;

[ExcludeFromCodeCoverage]
public partial class ApiTests : AppTestBase
{
    #region INFO

    [Fact]
    public async Task ShouldGetInfo()
    {
        // Arrange

        // Act
        var result = await Api.GetInfo(_mediator);

        // Assert
        var response = result as Ok<Models.ServerInfo>;
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Value.Should().NotBeNull();
        response.Value!.PublicKey.Should().Be(_certificateManager.PublicKey);
        response.Value.Address.Should().NotBeNullOrWhiteSpace();
        response.Value.GraphVersion.Should().NotBeNullOrWhiteSpace();
    }

    #endregion INFO

    #region SHARE

    [Fact]
    public async Task ShouldCreateSharedData()
    {
        // Arrange
        var sharedDataCreate = DataSeeder.ModelsFactory.CreateSharedDataCreate();

        // Act
        var result = await Api.PostShare(sharedDataCreate, _mediator);

        // Assert
        var response = result as Ok<Models.SharedData>;
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Value.Should().NotBeNull();
        response.Value!.ResourceUrl.Should().NotBeNullOrWhiteSpace();
        response.Value.Tag.Should().NotBeNullOrWhiteSpace();
        response.Value.Data.IsValidBase64().Should().BeTrue();
        UuidRegex().IsMatch(response.Value.Tag!).Should().BeTrue();
        SharedResourceUrl().IsMatch(response.Value!.ResourceUrl!).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotCreateSharedDataWithInvalidPublicKey()
    {
        // Arrange
        var sharedDataCreate = DataSeeder.ModelsFactory.CreateSharedDataCreate();
        sharedDataCreate.PublicKey = PKey.PublicKey2;

        // Act
        var result = await Api.PostShare(sharedDataCreate, _mediator);

        // Assert
        var response = result as ProblemHttpResult;
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ShouldNotCreateSharedDataWithInvalidData()
    {
        // Arrange
        var sharedDataCreate = DataSeeder.ModelsFactory.CreateSharedDataCreate();
        sharedDataCreate.SignedData = "invalid signed data";

        // Act
        var result = await Api.PostShare(sharedDataCreate, _mediator);

        // Assert
        var response = result as BadRequest;
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ShouldGetSharedData()
    {
        // Arrange
        var testData = DataSeeder.DataFactory.SharedData;
        
        // Act
        var result = await Api.GetShare(testData!.Tag, _mediator);

        // Assert
        var response = result as Ok<Models.SharedData>;
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(StatusCodes.Status200OK);
        response!.Value.Should().NotBeNull();
        response.Value!.Tag.Should().Be(testData.Tag);
        response.Value.Data.Should().Be(testData.Data);
        response.Value.ValidUntil.Should().BeNull();
        response.Value.ResourceUrl.Should().BeNull();
    }

    [Fact]
    public async Task ShouldRemoveSharedDataAfterAccessCountExceeded()
    {
        // Arrange
        var testData = DataSeeder.DataFactory.SharedData;
        
        // Act
        var result1 = await Api.GetShare(testData!.Tag, _mediator);
        var result2 = await Api.GetShare(testData!.Tag, _mediator);
        var result3 = await Api.GetShare(testData!.Tag, _mediator);

        // Assert
        var response1 = result1 as Ok<Models.SharedData>;
        var response2 = result2 as Ok<Models.SharedData>;
        var response3 = result3 as NotFound;
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        response3.Should().NotBeNull();
        response1!.StatusCode.Should().Be(StatusCodes.Status200OK);
        response2!.StatusCode.Should().Be(StatusCodes.Status200OK);
        response3!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetSharedDataReturnsNotFoundForInvalidTag()
    {
        // Arrange

        // Act
        var result = await Api.GetShare("not-existent-tag", _mediator);

        // Assert
        var response = result as NotFound;
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion SHARE

    #region VERTICES

    [Fact]
    public async Task ShouldGetVertices()
    {
        // Arrange

        // Act
        var result = await Api.GetVertices(_mediator);

        // Assert
        var response = result as Ok<List<Models.Vertex>>;
        response.Should().NotBeNull();
        response!.Value.Should().NotBeNull();
        response.Value!.Count.Should().Be(1);
        var vertex = response.Value.Single();
        vertex.PublicKey.Should().Be(_certificateManager.PublicKey);
        vertex.SignedData.IsValidBase64().Should().BeTrue();
        vertex.Neighborhood.Should().NotBeNull();
        vertex.Neighborhood!.Address.Should().Be(_certificateManager.Address);
        vertex.Neighborhood.Hostname.Should().Be(_configuration.GetHostname());
        vertex.Neighborhood.Neighbors.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetVertex()
    {
        // Arrange

        // Act
        var result = await Api.GetVertex(_certificateManager.Address, _mediator);

        // Assert
        var response = result as Ok<Models.Vertex>;
        response.Should().NotBeNull();
        response!.Value.Should().NotBeNull();
        response.Value!.PublicKey.Should().Be(_certificateManager.PublicKey);
        response.Value.SignedData.IsValidBase64().Should().BeTrue();
        response.Value.Neighborhood.Should().NotBeNull();
        response.Value.Neighborhood!.Address.Should().Be(_certificateManager.Address);
        response.Value.Neighborhood.Hostname.Should().Be(_configuration.GetHostname());
        response.Value.Neighborhood.Neighbors.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVertexReturnsNotFoundForWrongAddress()
    {
        // Arrange

        // Act
        var result = await Api.GetVertex(PKey.Address1, _mediator);

        // Assert
        result.Should().NotBeNull()
        .And.BeOfType<NotFound>()
        .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    #endregion VERTICES
    
    [GeneratedRegex(@"^http://localhost/Share\?Tag=[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$")]
    private static partial Regex SharedResourceUrl();

    [GeneratedRegex(@"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$")]
    private static partial Regex UuidRegex();
}
