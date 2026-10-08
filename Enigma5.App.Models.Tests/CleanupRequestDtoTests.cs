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

public class CleanupRequestDtoTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(long.MaxValue)]
    public void Validate_accepts_an_id_that_is_not_negative(long supId)
    {
        Assert.Empty(new CleanupRequestDto(supId).Validate());
    }

    [Fact]
    public void Validate_refuses_a_missing_id()
    {
        Assertions.SingleError(new CleanupRequestDto().Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(CleanupRequestDto.SupId));
    }

    [Fact]
    public void Validate_refuses_a_negative_id()
    {
        Assertions.SingleError(new CleanupRequestDto(-1).Validate(), ValidationErrorsDto.INVALID_VALUE_FOR_PROPERTY, nameof(CleanupRequestDto.SupId));
    }
}
