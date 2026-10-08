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

public class SharedDataCreateDtoTests
{
    [Fact]
    public void Validate_accepts_data_signed_with_the_given_key()
    {
        Assert.Empty(new SharedDataCreateDto(TestKeys.PublicKey3, SignedData.SharedPayloadSignedWithKey3).Validate());
        Assert.Empty(new SharedDataCreateDto(TestKeys.PublicKey3, SignedData.SharedPayloadSignedWithKey3, 5).Validate());
    }

    [Fact]
    public void The_access_count_is_one_unless_given()
    {
        Assert.Equal(1, new SharedDataCreateDto(TestKeys.PublicKey3, SignedData.SharedPayloadSignedWithKey3).AccessCount);
    }

    [Fact]
    public void Validate_refuses_data_signed_with_another_key()
    {
        var request = new SharedDataCreateDto(TestKeys.PublicKey1, SignedData.SharedPayloadSignedWithKey3);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.INVALID_SIGNATURE, nameof(SharedDataCreateDto.SignedData));
    }

    [Fact]
    public void Validate_refuses_base64_data_that_carries_no_signature()
    {
        var request = new SharedDataCreateDto(TestKeys.PublicKey3, "dGVzdC1zdHJpbmc=");

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.INVALID_SIGNATURE, nameof(SharedDataCreateDto.SignedData));
    }

    [Fact]
    public void Validate_refuses_data_that_is_not_base64()
    {
        var request = new SharedDataCreateDto(TestKeys.PublicKey3, "not base64!");

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(SharedDataCreateDto.SignedData));
    }

    [Fact]
    public void Validate_refuses_missing_data()
    {
        var request = new SharedDataCreateDto(TestKeys.PublicKey3);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(SharedDataCreateDto.SignedData));
    }

    [Fact]
    public void Validate_refuses_a_missing_public_key()
    {
        var request = new SharedDataCreateDto(null, SignedData.SharedPayloadSignedWithKey3);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(SharedDataCreateDto.PublicKey));
    }

    [Fact]
    public void Validate_refuses_a_public_key_that_is_not_in_PEM_form()
    {
        var request = new SharedDataCreateDto("not a key", SignedData.SharedPayloadSignedWithKey3);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(SharedDataCreateDto.PublicKey));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_refuses_an_access_count_below_one(int accessCount)
    {
        var request = new SharedDataCreateDto(TestKeys.PublicKey3, SignedData.SharedPayloadSignedWithKey3, accessCount);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.INVALID_VALUE_FOR_PROPERTY, nameof(SharedDataCreateDto.AccessCount));
    }
}
