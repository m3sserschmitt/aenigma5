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

using System.Text;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.Crypto.DataProviders;
using Enigma5.Structures;
using Microsoft.AspNetCore.SignalR.Client;
using Enigma5.App.Models;
using Enigma5.Crypto;
using System.Net.Http.Json;
using Enigma5.App.Models.HubInvocation;
using NSubstitute;
using Enigma5.Security.Contracts;

namespace Client;

public class Program
{
    private static async void HandleMessage(string message, string privateKey, string passphrase)
    {
        Console.WriteLine($"Message received");

        using var unsealer = SealProvider.Factory.CreateUnsealer(privateKey, Encoding.UTF8.GetBytes(passphrase));
        var certificateManager = Substitute.For<ICertificateManager>();
        certificateManager.CreateUnsealerAsync().Returns(unsealer);
        var onionParser = new OnionParser(certificateManager);

        if (await onionParser.ParseAsync(message))
        {
            Console.WriteLine($"Message: {Encoding.UTF8.GetString(onionParser.Content!, 0, onionParser.Content!.Length)}");
        }
        else
        {
            Console.WriteLine("There was an error on decrypting the message");
        }
    }

    private static async Task<string?> RequestPublicKey(string url)
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            return (await response.Content.ReadFromJsonAsync<ServerInfoDto>())?.PublicKey;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading server public key: {ex.Message}");
        }

        return null;
    }

    public static void HandleServerResponse(dynamic invocationResponse, string methodName)
    {
        if (!invocationResponse.Success)
        {
            var errors = invocationResponse.Errors as List<ErrorDto>;
            throw new Exception($"{methodName} method invocation failed; errors: {string.Join(", ", errors?.Select(error => error.Message) ?? [])}");
        }
    }

    public static async Task Main(string[] args)
    {
        string localAddress;
        string publicKey;
        string privateKey;
        string passphrase = PKey.Passphrase;

        string server = $"http://{args[1]}".Trim();
        string onionRoutingEndpoint = $"{server}/{Enigma5.App.Common.Constants.OnionRoutingEndpoint}";
        string serverInfoEndpoint = $"{server}/{Enigma5.App.Common.Constants.InfoEndpoint}";

        if (args[0] == "1")
        {
            localAddress = PKey.Address1;
            publicKey = PKey.PublicKey1;
            privateKey = PKey.PrivateKey1;
        }
        else
        {
            localAddress = PKey.Address2;
            publicKey = PKey.PublicKey2;
            privateKey = PKey.PrivateKey2;
        }

        var connection = new HubConnectionBuilder()
            .WithUrl(onionRoutingEndpoint, options =>
            {
                options.HttpMessageHandlerFactory = message =>
                {
                    if (message is HttpClientHandler clientHandler)
                        clientHandler.ServerCertificateCustomValidationCallback +=
                            (sender, certificate, chain, sslPolicyErrors) => { return true; };
                    return message;
                };
            })
            .Build();

        connection.On<RoutingRequestDto>(nameof(IEnigmaHub.RouteMessage), message =>
        {
            foreach (var payload in message.Payloads ?? [])
            {
                if (payload is not null)
                {
                    HandleMessage(payload, privateKey, passphrase);
                }
            }
        });

        connection.On<List<PendingMessageDto>>("Synchronize", messages =>
        {
            foreach (var message in messages)
            {
                if (message.Content != null)
                {
                    HandleMessage(message.Content, privateKey, passphrase);
                }
            }
        });

        await connection.StartAsync();

        var tokenResult = await connection.InvokeAsync<InvocationResultDto<string>>(nameof(IEnigmaHub.GenerateToken));
        HandleServerResponse(tokenResult, nameof(IEnigmaHub.GenerateToken));

        using var signature = SealProvider.Factory.CreateSigner(privateKey, Encoding.UTF8.GetBytes(passphrase));
        var encodedNonce = Convert.FromBase64String(tokenResult.Data!) ?? throw new Exception("Failed to base64 decode nonce.");
        var data = signature.Sign(encodedNonce) ?? throw new Exception("Nonce signature failed.");
        var authenticationResult = await connection.InvokeAsync<InvocationResultDto<bool>>(nameof(IEnigmaHub.Authenticate), new AuthenticationRequestDto(publicKey, Convert.ToBase64String(data)));
        HandleServerResponse(authenticationResult, nameof(IEnigmaHub.Authenticate));

        Console.WriteLine($"Authenticated.");

        var pullResult = await connection.InvokeAsync<InvocationResultDto<List<PendingMessageDto>>>(nameof(IEnigmaHub.Pull));
        HandleServerResponse(pullResult, nameof(IEnigmaHub.Pull));
        foreach (var pendingMessage in pullResult.Data!)
        {
            if (pendingMessage.Content != null)
            {
                HandleMessage(pendingMessage.Content, privateKey, passphrase);
            }
        }

        var message = "Test";
        var serverPublicKey = await RequestPublicKey(serverInfoEndpoint);
        
        while (args[0] == "1" && serverPublicKey != null)
        {
            var destinationPublicKey = PKey.PublicKey2;
            var destinationAddress = HashProvider.FromHexString(PKey.Address2);

            var onion = SealProvider.SealOnion(Encoding.UTF8.GetBytes(message), [destinationPublicKey, serverPublicKey], [PKey.Address1, PKey.Address2]);
            await connection.InvokeAsync(nameof(IEnigmaHub.RouteMessage), new RoutingRequestDto([onion!, onion]));

            Console.ReadLine();
        }

        Console.ReadLine();
        await connection.StopAsync();
    }
}
