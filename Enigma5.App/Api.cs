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

using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Handlers;
using Enigma5.App.Resources.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Enigma5.App;

public static class Api
{
    public static async Task<IResult> GetInfo(IMediator commandRouter)
    {
        var result = await commandRouter.Send(new GetServerInfoQuery());
        return result.CreateGetResponse();
    }

    public static async Task<IResult> PostShare(SharedDataCreate sharedDataCreate, IMediator commandRouter, IConfiguration configuration)
    {
        if (!sharedDataCreate.Valid)
        {
            return Results.BadRequest();
        }

        var result = await commandRouter.Send(new CreateSharedDataCommand(sharedDataCreate));
        return result.CreatePostResponse();
    }

    public static async Task<IResult> GetShare(string tag, IMediator commandRouter)
    {
        var sharedData = await commandRouter.Send(new GetSharedDataQuery(tag));

        if (!sharedData.IsSuccessNotNullResultValue())
        {
            return Results.NotFound();
        }

        var result = await commandRouter.Send(new IncrementSharedDataAccessCountCommand(sharedData.Value!.Tag!));

        if (result.IsSuccessNotNullResultValue() && result.Value!.AccessCount > result.Value.MaxAccessCount)
        {
            await commandRouter.Send(new RemoveSharedDataCommand(sharedData.Value.Tag!));
        }

        return sharedData.CreateGetResponse();
    }

    public static async Task<IResult> GetVertex(string address, IMediator commandRouter)
    {
        var result = await commandRouter.Send(new GetVertexQuery(address));
        return result.CreateGetResponse();
    }

    public static async Task<IResult> GetVertices(IMediator commandRouter)
    {
        var result = await commandRouter.Send(new GetVerticesQuery());
        return result.CreateGetResponse();
    }
}
