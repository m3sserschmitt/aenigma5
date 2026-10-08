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

namespace Enigma5.App.Models.Tests;

public class SignatureRequestDtoTests
{
    [Fact]
    public void Validate_accepts_a_base64_nonce()
    {
        Assert.Empty(new SignatureRequestDto("dGVzdC1zdHJpbmc=").Validate());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_refuses_a_missing_nonce(string? nonce)
    {
        Assertions.SingleError(new SignatureRequestDto(nonce).Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(SignatureRequestDto.Nonce));
    }

    [Fact]
    public void Validate_refuses_a_nonce_that_is_not_base64()
    {
        Assertions.SingleError(new SignatureRequestDto("not base64!").Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(SignatureRequestDto.Nonce));
    }
}
