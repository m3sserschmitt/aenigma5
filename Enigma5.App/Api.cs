/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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
using Microsoft.AspNetCore.Mvc;

namespace Enigma5.App;

public static class Api
{
    public static async Task<IResult> GetInfo([FromServices] IMediator commandRouter)
    {
        var result = await commandRouter.Send(new GetServerInfoQuery());
        return result.CreateGetResponse();
    }

    public static async Task<IResult> PostShare(
        [FromBody] SharedDataCreateDto sharedDataCreate,
        [FromServices] IMediator commandRouter)
    {
        var errors = sharedDataCreate.Validate();
        if (errors.Count > 0)
        {
            return Results.BadRequest(errors);
        }

        var result = await commandRouter.Send(new CreateSharedDataCommand(sharedDataCreate));
        return result.CreatePostResponse();
    }

    public static async Task<IResult> GetShare(
        [FromQuery] string? tag,
        [FromServices] IMediator commandRouter)
    {
        if (tag is null)
        {
            return Results.BadRequest();
        }

        var sharedData = await commandRouter.Send(new GetSharedDataQuery(tag));
        return sharedData.IsSuccessNotNullResultValue() ? sharedData.CreateGetResponse() : Results.NotFound();
    }

    public static async Task<IResult> IncrementSharedDataAccessCount(
        [FromQuery] string? tag,
        [FromServices] IMediator commandRouter)
    {
        if (tag is null)
        {
            return Results.BadRequest();
        }

        var result = await commandRouter.Send(new IncrementSharedDataAccessCountCommand(tag));
        return result.CreatePutResponse();
    }

    public static async Task<IResult> GetVertex(
        [FromQuery] string? address,
        [FromServices] IMediator commandRouter)
    {
        if (address is null)
        {
            return Results.BadRequest();
        }

        var result = await commandRouter.Send(new GetVertexQuery(address));
        return result.CreateGetResponse();
    }

    public static async Task<IResult> GetVertices([FromServices] IMediator commandRouter)
    {
        var result = await commandRouter.Send(new GetVerticesQuery());
        return result.CreateGetResponse();
    }

    public static async Task<IResult> PostFile(
        [FromForm] IFormFile file,
        [FromForm] int maxAccessCount,
        [FromServices] IMediator commandRouter)
    {
        if (file == null || file.Length == 0)
            return Results.BadRequest();

        var result = await commandRouter.Send(new CreateFileCommand(file, maxAccessCount));
        return result.CreatePostResponse();
    }

    public static async Task<IResult> GetFile(
        [FromQuery] string? tag,
        [FromServices] IMediator commandRouter)
    {
        if (tag is null)
        {
            return Results.BadRequest();
        }

        var result = await commandRouter.Send(new GetFileQuery(tag));

        if (!result.IsSuccessNotNullResultValue() || result.Value?.File is null)
        {
            return Results.NotFound();
        }

        return Results.File(
            fileStream: result.Value!.File,
            fileDownloadName: result.Value.Tag
        );
    }

    public static async Task<IResult> IncrementFileAccessCount(
        [FromQuery] string? tag,
        [FromServices] IMediator commandRouter)
    {
        if (tag is null)
        {
            return Results.BadRequest();
        }

        var result = await commandRouter.Send(new IncrementFileAccessCountCommand(tag));
        return result.CreatePutResponse();
    }
}
