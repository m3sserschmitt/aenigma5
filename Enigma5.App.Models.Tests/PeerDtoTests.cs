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

public class PeerDtoTests
{
    [Fact]
    public void Two_peers_are_equal_when_their_addresses_are_equal()
    {
        var stored = new PeerDto { Id = 1, Host = "http://one.example", Address = TestKeys.Address1 };
        var seen = new PeerDto { Host = "http://two.example", Address = TestKeys.Address1, Connected = true };

        Assert.True(stored == seen);
        Assert.Equal(stored.GetHashCode(), seen.GetHashCode());
        Assert.False(stored == new PeerDto { Id = 1, Host = "http://one.example", Address = TestKeys.Address2 });
    }

    [Fact]
    public void A_set_keeps_one_peer_for_each_address()
    {
        var peers = new HashSet<PeerDto>
        {
            new() { Id = 1, Address = TestKeys.Address1 },
            new() { Address = TestKeys.Address1, Connected = true },
            new() { Address = TestKeys.Address2 }
        };

        Assert.Equal(2, peers.Count);
    }
}
