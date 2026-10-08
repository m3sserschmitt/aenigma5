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

using Enigma5.Tests.Base;

namespace Enigma5.App.Models.Tests;

public class AuthenticationRequestDtoTests
{
    private const string Signature = "dGVzdC1zdHJpbmc=";

    [Fact]
    public void Validate_accepts_a_public_key_and_a_base64_signature()
    {
        Assert.Empty(new AuthenticationRequestDto(TestKeys.PublicKey1, Signature).Validate());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Validate_refuses_a_missing_public_key(string? publicKey)
    {
        Assertions.SingleError(new AuthenticationRequestDto(publicKey, Signature).Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(AuthenticationRequestDto.PublicKey));
    }

    [Fact]
    public void Validate_refuses_a_public_key_that_is_not_in_PEM_form()
    {
        Assertions.SingleError(new AuthenticationRequestDto("not a key", Signature).Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(AuthenticationRequestDto.PublicKey));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_refuses_a_missing_signature(string? signature)
    {
        Assertions.SingleError(new AuthenticationRequestDto(TestKeys.PublicKey1, signature).Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(AuthenticationRequestDto.Signature));
    }

    [Fact]
    public void Validate_refuses_a_signature_that_is_not_base64()
    {
        Assertions.SingleError(new AuthenticationRequestDto(TestKeys.PublicKey1, "not base64!").Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(AuthenticationRequestDto.Signature));
    }

    [Fact]
    public void Validate_reports_both_missing_values_in_one_error()
    {
        var error = Assert.Single(new AuthenticationRequestDto().Validate());

        Assert.Equal(ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, error.Message);
        Assert.Equal([nameof(AuthenticationRequestDto.PublicKey), nameof(AuthenticationRequestDto.Signature)], error.Properties!.Order());
    }
}
