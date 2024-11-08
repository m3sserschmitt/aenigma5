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
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Hangfire;

[ExcludeFromCodeCoverage]
public class MediatorHangfireBridge(
    IMediator mediator,
    ILogger<MediatorHangfireBridge> logger
    )
{
    private readonly IMediator _mediator = mediator;

    private readonly ILogger<MediatorHangfireBridge> _logger = logger;

    public Task Send<T>(IRequest<T> command)
    {
        _logger.LogInformation("Executing {CommandName} for Hangfire Job: {@Command}", command.GetType().Name, command);
        return _mediator.Send(command);
    }
}
