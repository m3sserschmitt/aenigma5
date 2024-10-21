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

using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Enigma5.App.Models.HubInvocation;
using Enigma5.Crypto;
using Microsoft.AspNetCore.SignalR.Client;

namespace Enigma5.Security.Extensions;

public static class HubConnectionExtensions
{
    public static async Task<bool> AuthenticateAsync(
        this HubConnection connection,
        CertificateManager certificateManager,
        bool syncOnSuccess = false,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var nonce = await connection.InvokeAsync<InvocationResult<string>>(nameof(IHub.GenerateToken), cancellationToken);

            if (!nonce.Success || nonce.Data is null)
            {
                return false;
            }

            using var signature = Envelope.Factory.CreateSignature(certificateManager.PrivateKey, string.Empty);
            var data = signature.Sign(Convert.FromBase64String(nonce.Data));

            if (data is null)
            {
                return false;
            }

            var authentication = await connection.InvokeAsync<InvocationResult<bool>>(nameof(IHub.Authenticate), new AuthenticationRequest
            {
                PublicKey = certificateManager.PublicKey,
                Signature = Convert.ToBase64String(data),
                SyncMessagesOnSuccess = syncOnSuccess,
            }, cancellationToken);

            return authentication.Success && authentication.Data;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
