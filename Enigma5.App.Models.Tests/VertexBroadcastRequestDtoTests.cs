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

using Enigma5.App.Models.Tests.TestData;
using Enigma5.Tests.Base;

namespace Enigma5.App.Models.Tests;

public class VertexBroadcastRequestDtoTests
{
    [Fact]
    public void A_signed_neighborhood_is_read_from_the_signed_data()
    {
        var request = new VertexBroadcastRequestDto(TestKeys.PublicKey1, SignedData.NeighborhoodSignedWithKey1);

        Assert.Empty(request.Validate());
        Assert.Equal(TestKeys.Address1, request.Neighborhood.Address);
        Assert.Equal("http://adjacent.example", request.Neighborhood.Hostname);
        Assert.Equal([TestKeys.Address2], request.Neighborhood.Neighbors);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), request.Neighborhood.LastUpdate);
    }

    // The model only separates the data from the signature, whose length follows from the key size.
    // Whether the signature is right is checked later, by NetworkGraphValidationPolicy.
    [Fact]
    public void The_signature_itself_is_not_checked_by_the_model()
    {
        var request = new VertexBroadcastRequestDto(TestKeys.PublicKey2, SignedData.NeighborhoodSignedWithKey1);

        Assert.Empty(request.Validate());
        Assert.Equal(TestKeys.Address1, request.Neighborhood.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("dGVzdC1zdHJpbmc=")]
    public void Data_that_is_missing_or_carries_no_signature_is_dropped(string? signedData)
    {
        var request = new VertexBroadcastRequestDto(TestKeys.PublicKey1, signedData);

        Assert.Null(request.SignedData);
        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PROPERTIES_FORMAT_COULD_NOT_BE_VERIFIED, nameof(VertexBroadcastRequestDto.SignedData));
    }

    [Fact]
    public void Validate_refuses_a_missing_public_key()
    {
        var request = new VertexBroadcastRequestDto(null, SignedData.NeighborhoodSignedWithKey1);

        Assert.Equal(
            [ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, ValidationErrorsDto.PROPERTIES_FORMAT_COULD_NOT_BE_VERIFIED],
            request.Validate().Select(error => error.Message).Order());
    }

    [Fact]
    public void Validate_reports_the_errors_of_the_signed_neighborhood()
    {
        var request = new VertexBroadcastRequestDto(TestKeys.PublicKey1, SignedData.NeighborhoodWithBadNeighborSignedWithKey1);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(NeighborhoodDto.Neighbors));
    }
}
