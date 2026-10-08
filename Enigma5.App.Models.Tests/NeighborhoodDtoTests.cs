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

public class NeighborhoodDtoTests
{
    [Fact]
    public void Validate_accepts_an_address_with_neighbors()
    {
        Assert.Empty(new NeighborhoodDto(TestKeys.Address1, neighbors: [TestKeys.Address2, TestKeys.Address3]).Validate());
    }

    [Fact]
    public void Validate_accepts_an_empty_list_of_neighbors()
    {
        Assert.Empty(new NeighborhoodDto(TestKeys.Address1, neighbors: []).Validate());
    }

    [Fact]
    public void Validate_refuses_a_missing_address()
    {
        Assertions.SingleError(new NeighborhoodDto(neighbors: []).Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(NeighborhoodDto.Address));
    }

    [Fact]
    public void Validate_refuses_an_address_in_the_wrong_form()
    {
        Assertions.SingleError(new NeighborhoodDto("abc", neighbors: []).Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(NeighborhoodDto.Address));
    }

    [Fact]
    public void Validate_refuses_a_missing_list_of_neighbors()
    {
        Assertions.SingleError(new NeighborhoodDto(TestKeys.Address1).Validate(), ValidationErrorsDto.NULL_REQUIRED_PROPERTIES, nameof(NeighborhoodDto.Neighbors));
    }

    [Fact]
    public void Validate_refuses_a_neighbor_that_is_not_an_address()
    {
        var neighborhood = new NeighborhoodDto(TestKeys.Address1, neighbors: [TestKeys.Address2, "not-an-address"]);

        Assertions.SingleError(neighborhood.Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(NeighborhoodDto.Neighbors));
    }
}
