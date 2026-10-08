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

public class TriggerBroadcastRequestDtoTests
{
    [Fact]
    public void Validate_accepts_no_addresses()
    {
        Assert.Empty(new TriggerBroadcastRequestDto().Validate());
        Assert.Empty(new TriggerBroadcastRequestDto([]).Validate());
    }

    [Fact]
    public void Validate_accepts_valid_addresses()
    {
        Assert.Empty(new TriggerBroadcastRequestDto([TestKeys.Address1, TestKeys.Address2]).Validate());
    }

    [Fact]
    public void Validate_refuses_a_list_with_a_value_that_is_not_an_address()
    {
        var request = new TriggerBroadcastRequestDto([TestKeys.Address1, "not-an-address"]);

        Assertions.SingleError(request.Validate(), ValidationErrorsDto.PROPERTIES_NOT_IN_CORRECT_FORMAT, nameof(TriggerBroadcastRequestDto.NewAddresses));
    }
}
