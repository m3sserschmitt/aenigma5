/*
    Aenigma - Onion Routing based messaging application
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

using System.Text;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.Crypto.DataProviders;
using Enigma5.Structures;
using Microsoft.AspNetCore.SignalR.Client;
using Enigma5.App.Models;
using Enigma5.Crypto;
using System.Net.Http.Json;
using Enigma5.App.Common.Constants;
using Enigma5.App.Models.HubInvocation;

namespace Client;

public class Program
{
    private static void HandleMessage(string message, string privateKey, string passphrase)
    {
        Console.WriteLine($"Message received");
        var decodedData = Convert.FromBase64String(message);

        using var onionParser = OnionParser.Factory.Create(Encoding.UTF8.GetBytes(privateKey), passphrase);

        if (onionParser.Parse(new Onion { Content = decodedData }))
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
            return (await response.Content.ReadFromJsonAsync<ServerInfo>())?.PublicKey;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading server public key: {ex.Message}");
        }

        return null;
    }

    public static void HandleServerResponse(dynamic invocationResponse, string methodName)
    {
        if(!invocationResponse.Success)
        {
            var errors = invocationResponse.Errors as List<Error>;
            throw new Exception($"{methodName} method invocation failed; errors: {string.Join(", ", errors?.Select(error => error.Message) ?? [])}");
        }
    }

    public static async Task Main(string[] args)
    {
        string publicKey;
        string privateKey;
        string passphrase = PKey.Passphrase;

        string server = $"http://{args[1]}".Trim();
        string onionRoutingEndpoint = $"{server}/{Endpoints.OnionRoutingEndpoint}";
        string serverInfoEndpoint = $"{server}/{Endpoints.ServerInfoEndpoint}";

        if (args[0] == "1")
        {
            publicKey = PKey.PublicKey1;
            privateKey = PKey.PrivateKey1;
        }
        else
        {
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

        connection.On<RoutingRequest>(nameof(IHub.RouteMessage), message =>
        {
            HandleMessage(message.Payload!, privateKey, passphrase);
        });

        connection.On<List<PendingMessage>>("Synchronize", messages =>
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

        var tokenResult = await connection.InvokeAsync<InvocationResult<string>>(nameof(IHub.GenerateToken));
        HandleServerResponse(tokenResult, nameof(IHub.GenerateToken));

        using var signature = Envelope.Factory.CreateSignature(Encoding.UTF8.GetBytes(privateKey), passphrase);
        var encodedNonce = Convert.FromBase64String(tokenResult.Data!) ?? throw new Exception("Failed to base64 decode nonce.");
        var data = signature.Sign(encodedNonce) ?? throw new Exception("Nonce signature failed.");

        var authenticationResult = await connection.InvokeAsync<InvocationResult<bool>>(nameof(IHub.Authenticate), new AuthenticationRequest
        {
            PublicKey = publicKey,
            Signature = Convert.ToBase64String(data),
            SyncMessagesOnSuccess = true,
        });
        HandleServerResponse(authenticationResult, nameof(IHub.Authenticate));

        Console.WriteLine($"Authenticated.");


        var message = "Test";
        var serverPublicKey = await RequestPublicKey(serverInfoEndpoint);

        while (args[0] == "1" && serverPublicKey != null)
        {
            var destinationPublicKey = PKey.PublicKey2;
            var destinationAddress = HashProvider.FromHexString(PKey.Address2);
            var localAddress = HashProvider.FromHexString(PKey.Address1);

            var onion = OnionBuilder
                .Create()
                .SetMessageContent(Encoding.UTF8.GetBytes(message))
                .SetNextAddress(localAddress)
                .Seal(destinationPublicKey)
                .AddPeel()
                .SetNextAddress(destinationAddress)
                .Seal(serverPublicKey)
                .Build();
            await connection.InvokeAsync(nameof(IHub.RouteMessage), new RoutingRequest(Convert.ToBase64String(onion.Content)));

            Console.ReadLine();
        }

        Console.ReadLine();
        await connection.StopAsync();
    }
}
