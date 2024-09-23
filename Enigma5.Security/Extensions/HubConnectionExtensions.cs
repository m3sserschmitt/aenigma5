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
