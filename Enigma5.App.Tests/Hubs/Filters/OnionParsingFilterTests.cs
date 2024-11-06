/*
    Aenigma - Federal messaging system
    Copyright (C) 2024  Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using System.Diagnostics.CodeAnalysis;
using Enigma5.App.Hubs.Filters;
using Enigma5.App.Models;
using Enigma5.App.Tests.Helpers;
using Enigma5.Crypto.DataProviders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Enigma5.App.Tests.Hubs.Filters;

[ExcludeFromCodeCoverage]
public class OnionParsingFilterTests : FiltersTestBase<OnionParsingFilter>
{
    [Fact]
    public async Task ShouldParseOnion()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var onion = DataSeeder.ModelsFactory.CreateOnion(data);
        _hubMethodArguments[0].Returns(new RoutingRequest { Payload = onion });

        // Act
        await _filter.Handle(_hubInvocationContext, _ => ValueTask.FromResult<object?>(default));

        // Assert
        _hub.Next.Should().Be(PKey.Address2);
    }
}
