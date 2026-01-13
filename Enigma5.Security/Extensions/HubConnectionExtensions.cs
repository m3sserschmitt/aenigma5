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

using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.Security.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace Enigma5.Security.Extensions;

public static class HubConnectionExtensions
{
    public static async Task<bool> AuthenticateAsync(
        this HubConnection connection,
        ICertificateManager certificateManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var publicKey = await certificateManager.GetPublicKeyAsync();
            if(string.IsNullOrWhiteSpace(publicKey))
            {
                return false;
            }

            var nonce = await connection.InvokeAsync<InvocationResultDto<string>>(nameof(IEnigmaHub.GenerateToken), cancellationToken);

            if (!nonce.Success || nonce.Data is null)
            {
                return false;
            }

            using var signer = await certificateManager.CreateSignerAsync();
            var data = signer.Sign(Convert.FromBase64String(nonce.Data));

            if (data is null)
            {
                return false;
            }

            var authentication = await connection.InvokeAsync<InvocationResultDto<bool>>(
                nameof(IEnigmaHub.Authenticate),
                new AuthenticationRequestDto(publicKey, Convert.ToBase64String(data)),
                cancellationToken);

            return authentication.Success && authentication.Data;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
