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
using Enigma5.Tests.Base;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enigma5.App.Tests.Resources.Handlers;

[ExcludeFromCodeCoverage]
public class CleanupSharedDataHandlerTests : HandlerTestBase<CleanupSharedDataHandler>
{
    [Fact]
    public async Task ShouldCleanupOldSharedData()
    {
        // Arrange
        var command = new CleanupSharedDataCommand(TimeSpan.FromDays(1));

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CommandResult<int>>();
        result.Success.Should().BeTrue();
        result.Value.Should().Be(1);
        (await _dbContext.SharedData.FirstOrDefaultAsync(item => item.Tag == DataSeeder.DataFactory.SharedData.Tag)).Should().NotBeNull();
        (await _dbContext.SharedData.FirstOrDefaultAsync(item => item.Tag == DataSeeder.DataFactory.OldSharedData.Tag)).Should().BeNull();
    }
}
