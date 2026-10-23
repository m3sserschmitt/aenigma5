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

namespace Enigma5.App.Common.Tests;

public class ConstantsTests
{
    // Enigma5.Docs, Chapter 5, shows how these two limits were calculated.
    [Fact]
    public void The_size_limits_of_the_protocol_have_their_published_values()
    {
        Assert.Equal(16384, Constants.MaxOnionSize);
        Assert.Equal(364544, Constants.SignalRMaximumReceiveMessageSize);
        Assert.Equal(4096, Constants.MaxPublicKeySize);
    }

    // The largest message of the client: a payload of 5,646 bytes in 7 layers of 574 bytes (4096-bit keys).
    [Fact]
    public void The_largest_onion_of_a_client_fits_the_limit_of_one_onion()
    {
        var bytes = 5646 + 7 * (2 + Constants.MaxPublicKeySize / 8 + 12 + 16 + 32);
        var characters = 4 * ((bytes + 2) / 3);

        Assert.Equal(12888, characters);
        Assert.True(characters <= Constants.MaxOnionSize);
    }

    // The worst case of JSON serialization: "/" written as \/ and "+" as \u002B, each about 1/64 of an onion,
    // both "=" at the end as \u003D, and the quotes and the comma of each payload.
    [Fact]
    public void A_full_batch_of_the_largest_onions_fits_one_hub_message_in_the_worst_case_of_serialization()
    {
        var onion = Constants.MaxOnionSize + Constants.MaxOnionSize / 64 * (1 + 5) + 2 * 5 + 3;
        var batch = Constants.MessagesPageSize * onion;

        Assert.Equal(358660, batch);
        Assert.True(batch < Constants.SignalRMaximumReceiveMessageSize);
    }
}
