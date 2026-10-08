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

using System.Net;
using Enigma5.App.Common.Extensions;

namespace Enigma5.App.Common.Tests.Extensions;

public class IPAddressExtensionsTests
{
    [Fact]
    public void Normalize_turns_an_IPv4_address_mapped_to_IPv6_into_IPv4()
    {
        Assert.Equal(IPAddress.Parse("192.168.1.10"), IPAddress.Parse("::ffff:192.168.1.10").Normalize());
    }

    [Theory]
    [InlineData("192.168.1.10")]
    [InlineData("::1")]
    [InlineData("2001:db8::1")]
    public void Normalize_leaves_other_addresses_unchanged(string address)
    {
        Assert.Equal(IPAddress.Parse(address), IPAddress.Parse(address).Normalize());
    }
}
