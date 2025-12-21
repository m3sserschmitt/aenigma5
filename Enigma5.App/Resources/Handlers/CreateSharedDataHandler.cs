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

using Enigma5.App.Common.Extensions;
using Enigma5.App.Resources.Commands;
using Enigma5.Crypto;
using Enigma5.Crypto.Extensions;
using MediatR;

namespace Enigma5.App.Resources.Handlers;

public class CreateSharedDataHandler(
    Data.EnigmaDbContext context,
    IConfiguration configuration)
    : IRequestHandler<CreateSharedDataCommand, CommandResult<Models.SharedDataDto>>
{
    private readonly Data.EnigmaDbContext _context = context;

    private readonly IConfiguration _configuration = configuration;

    public async Task<CommandResult<Models.SharedDataDto>> Handle(CreateSharedDataCommand request, CancellationToken cancellationToken)
    {
        if (!request.SharedDataCreate.PublicKey.IsValidPublicKey() || !request.SharedDataCreate.SignedData.IsValidBase64())
        {
            return CommandResult.CreateResultFailure<Models.SharedDataDto>();
        }

        using var signatureVerification = SealProvider.Factory.CreateVerifier(request.SharedDataCreate.PublicKey!);

        if (signatureVerification is null)
        {
            return CommandResult.CreateResultFailure<Models.SharedDataDto>();
        }

        var decodedSignature = Convert.FromBase64String(request.SharedDataCreate.SignedData!);

        if (decodedSignature is null || decodedSignature.Length == 0 || !signatureVerification.Verify(decodedSignature))
        {
            return CommandResult.CreateResultFailure<Models.SharedDataDto>();
        }

        var sharedData = new Data.SharedData
        {
            Data = request.SharedDataCreate.SignedData,
            PublicKey = request.SharedDataCreate.PublicKey,
            MaxAccessCount = request.SharedDataCreate.AccessCount
        };
        await _context.AddAsync(sharedData, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        var hostname = _configuration.GetHostname();

        return CommandResult.CreateResultSuccess(new Models.SharedDataDto
        {
            Tag = sharedData.Tag,
            ResourceUrl = hostname is not null ? $"{hostname}/{Common.Constants.ShareEndpoint}?Tag={sharedData.Tag}" : null,
            ValidUntil = DateTimeOffset.Now + _configuration.GetSharedDataRetentionPeriod()
        });
    }
}
