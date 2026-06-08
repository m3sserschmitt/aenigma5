/*
    Aenigma - Federated messaging system
    Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Data;
using Enigma5.App.Models;
using Enigma5.App.Resources.Commands;
using Enigma5.App.Resources.Contracts;
using Enigma5.Crypto;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreateSharedDataHandler(
    IConfiguration configuration,
    IDbWriter dbWriter
) : IRequestHandler<CreateSharedDataCommand, CommandResult<SharedDataDto>>
{
    private readonly IConfiguration _configuration = configuration;

    private readonly IDbWriter _dbWriter = dbWriter;

    public async Task<CommandResult<SharedDataDto>> Handle(CreateSharedDataCommand request, CancellationToken cancellationToken)
    {
        if (!request.SharedDataCreate.PublicKey.IsValidPublicKey() || !request.SharedDataCreate.SignedData.IsValidBase64())
        {
            return CommandResult.CreateResultFailure<SharedDataDto>();
        }

        using var signatureVerification = SealProvider.Factory.CreateVerifier(request.SharedDataCreate.PublicKey!);

        if (signatureVerification is null)
        {
            return CommandResult.CreateResultFailure<SharedDataDto>();
        }

        var decodedSignature = Convert.FromBase64String(request.SharedDataCreate.SignedData!);

        if (decodedSignature is null || decodedSignature.Length == 0 || !signatureVerification.Verify(decodedSignature))
        {
            return CommandResult.CreateResultFailure<SharedDataDto>();
        }

        var sharedData = new SharedData
        {
            Data = request.SharedDataCreate.SignedData,
            PublicKey = request.SharedDataCreate.PublicKey,
            MaxAccessCount = request.SharedDataCreate.AccessCount ?? 1
        };

        return await _dbWriter.CreateSharedDataAsync(sharedData, cancellationToken) > 0 ?
        CommandResult.CreateResultSuccess(new SharedDataDto
        {
            Tag = sharedData.Tag,
            ResourceUrl = _configuration.GetSharedDataUrl(sharedData.Tag),
            ValidUntil = _configuration.GetSharedDataValidityDate()
        }) : CommandResult.CreateResultFailure<SharedDataDto>();
    }
}
