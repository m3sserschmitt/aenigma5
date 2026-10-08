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

using Enigma5.App.Common;

namespace Enigma5.App.Models.Tests;

public class RoutingRequestDtoTests
{
    private const string Payload = "dGVzdC1zdHJpbmc=";

    private const string Uuid = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    private static List<string?> Payloads(int count, string payload = Payload) => [.. Enumerable.Repeat<string?>(payload, count)];

    [Fact]
    public void Validate_accepts_a_single_payload_without_uuid()
    {
        Assert.Empty(new RoutingRequestDto([Payload]).Validate());
    }

    [Fact]
    public void Validate_accepts_a_single_payload_with_a_uuid()
    {
        Assert.Empty(new RoutingRequestDto([Payload], Uuid).Validate());
        Assert.Empty(new RoutingRequestDto([Payload], Uuid.ToUpperInvariant()).Validate());
    }

    [Fact]
    public void Validate_accepts_the_largest_number_of_payloads()
    {
        Assert.Empty(new RoutingRequestDto(Payloads(Constants.MessagesPageSize)).Validate());
    }

    [Fact]
    public void Validate_accepts_a_payload_of_the_largest_size()
    {
        Assert.Empty(new RoutingRequestDto([new string('A', Constants.MaxOnionSize)]).Validate());
    }

    [Fact]
    public void Validate_refuses_missing_payloads()
    {
        Assertions.SingleError(new RoutingRequestDto(null).Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(RoutingRequestDto.Payloads));
        Assertions.SingleError(new RoutingRequestDto([]).Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(RoutingRequestDto.Payloads));
    }

    [Fact]
    public void Validate_refuses_more_payloads_than_one_page()
    {
        var request = new RoutingRequestDto(Payloads(Constants.MessagesPageSize + 1));

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.TOO_MANY_PAYLOADS, nameof(RoutingRequestDto.Payloads));
    }

    [Fact]
    public void Validate_refuses_a_payload_above_the_largest_size()
    {
        var request = new RoutingRequestDto([Payload, new string('A', Constants.MaxOnionSize + 4)]);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PAYLOAD_TOO_LARGE, nameof(RoutingRequestDto.Payloads));
    }

    [Theory]
    [InlineData("not base64!")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_refuses_a_payload_that_is_not_base64(string? payload)
    {
        var request = new RoutingRequestDto([Payload, payload]);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(RoutingRequestDto.Payloads));
    }

    [Fact]
    public void Validate_refuses_a_uuid_with_several_payloads()
    {
        var request = new RoutingRequestDto(Payloads(2), Uuid);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.UUID_NOT_ALLOWED_FOR_MULTIPLE_PAYLOADS, nameof(RoutingRequestDto.Uuid));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("3f2504e0-4f89-11d3-9a0c")]
    public void Validate_refuses_a_uuid_that_is_not_a_guid(string uuid)
    {
        var request = new RoutingRequestDto([Payload], uuid);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(RoutingRequestDto.Uuid));
    }
}
