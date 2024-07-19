using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.App.Models;
using Enigma5.Crypto;
using Microsoft.AspNetCore.SignalR.Client;

namespace Enigma5.Security.Extensions;

public static class HubConnectionExtensions
{
    public static async Task<bool> AuthenticateAsync(
        this HubConnection connection,
        CertificateManager certificateManager,
        bool syncOnSuccess = false,
        bool updateNetworkGraph = false
    )
    {
        var token = await connection.InvokeAsync<string?>(nameof(IHub.GenerateToken));

        if (token is null)
        {
            return false;
        }

        using var signature = Envelope.Factory.CreateSignature(certificateManager.PrivateKey, string.Empty);
        var data = signature.Sign(Convert.FromBase64String(token));

        if (data is null)
        {
            return false;
        }

        return await connection.InvokeAsync<bool>(nameof(IHub.Authenticate), new AuthenticationRequest
        {
            PublicKey = certificateManager.PublicKey,
            Signature = Convert.ToBase64String(data),
            SyncMessagesOnSuccess = syncOnSuccess,
            UpdateNetworkGraph = updateNetworkGraph
        });
    }
}
