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
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enigma5.App.Tests.Resources.Handlers;

[ExcludeFromCodeCoverage]
public class CleanupMessagesHandlerTests : HandlerTestBase<CleanupMessagesHandler>
{
    [Fact]
    public async Task ShouldCleanupOldMessages()
    {
        // Arrange
        var request = new CleanupMessagesCommand(TimeSpan.FromMinutes(15), false);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CommandResult<int>>();
        result.Success.Should().BeTrue();
        result.Value.Should().Be(1);
        (await _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataSeeder.DataFactory.PendingMesage.Id)).Should().NotBeNull();
        (await _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataSeeder.DataFactory.DeliveredPendingMesage.Id)).Should().NotBeNull();
        (await _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataSeeder.DataFactory.OldPendingMesage.Id)).Should().BeNull();
    }

    [Fact]
    public async Task ShouldCleanupDeliveredMessages()
    {
        // Arrange
        var request = new CleanupMessagesCommand(TimeSpan.FromMinutes(15), true);

        // Act
        var result = await _handler.Handle(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CommandResult<int>>();
        result.Success.Should().BeTrue();
        result.Value.Should().Be(2);
        (await _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataSeeder.DataFactory.PendingMesage.Id)).Should().NotBeNull();
        (await _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataSeeder.DataFactory.DeliveredPendingMesage.Id)).Should().BeNull();
        (await _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataSeeder.DataFactory.OldPendingMesage.Id)).Should().BeNull();
    }
}
