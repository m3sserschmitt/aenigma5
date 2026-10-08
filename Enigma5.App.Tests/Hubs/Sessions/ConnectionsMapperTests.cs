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

using Enigma5.App.Hubs.Sessions;
using Enigma5.Tests.Base;

namespace Enigma5.App.Tests.Hubs.Sessions;

public class ConnectionsMapperTests
{
    private readonly ConnectionsMapper _mapper = new();

    [Fact]
    public void A_stored_pair_is_found_from_both_sides()
    {
        Assert.True(_mapper.TryAdd(TestKeys.Address1, "connection-1"));

        Assert.True(_mapper.TryGetConnectionId(TestKeys.Address1, out var connectionId));
        Assert.Equal("connection-1", connectionId);
        Assert.True(_mapper.TryGetAddress("connection-1", out var address));
        Assert.Equal(TestKeys.Address1, address);
    }

    [Fact]
    public void A_second_connection_for_an_address_replaces_the_first()
    {
        _mapper.TryAdd(TestKeys.Address1, "connection-1");

        Assert.True(_mapper.TryAdd(TestKeys.Address1, "connection-2"));

        Assert.True(_mapper.TryGetConnectionId(TestKeys.Address1, out var connectionId));
        Assert.Equal("connection-2", connectionId);
        Assert.False(_mapper.TryGetAddress("connection-1", out _));
        Assert.Single(_mapper.Connections);
    }

    [Fact]
    public void Remove_takes_the_pair_out_and_gives_its_address()
    {
        _mapper.TryAdd(TestKeys.Address1, "connection-1");
        _mapper.TryAdd(TestKeys.Address2, "connection-2");

        Assert.True(_mapper.Remove("connection-1", out var address));

        Assert.Equal(TestKeys.Address1, address);
        Assert.False(_mapper.TryGetConnectionId(TestKeys.Address1, out _));
        Assert.True(_mapper.TryGetConnectionId(TestKeys.Address2, out _));
    }

    [Fact]
    public void An_unknown_connection_or_address_is_not_found()
    {
        Assert.False(_mapper.Remove("unknown", out var address));
        Assert.Null(address);
        Assert.False(_mapper.TryGetAddress("unknown", out _));
        Assert.False(_mapper.TryGetConnectionId(TestKeys.Address1, out _));
    }
}
