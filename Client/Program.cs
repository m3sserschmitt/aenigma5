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
using System.Text.Json;

namespace Client;

public class Program
{
    private static void HandleMessage(string message, string privateKey, string passphrase)
    {
        Console.WriteLine($"Message received");

        using var unsealer = SealProvider.Factory.CreateUnsealer(privateKey, passphrase);
        var onionParser = new OnionParser(unsealer);

        if (onionParser.Parse(message))
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
        if (!invocationResponse.Success)
        {
            var errors = invocationResponse.Errors as List<Error>;
            throw new Exception($"{methodName} method invocation failed; errors: {string.Join(", ", errors?.Select(error => error.Message) ?? [])}");
        }
    }

    private static VertexBroadcastRequest CreateVertexBroadcast(string localAddress, string serverAddress, string publicKey, string privateKey, string passphrase)
    {
        var neighborhood = new AdjacencyList()
        {
            Address = localAddress,
            Hostname = null,
            Neighbors = [serverAddress]
        };
        var serializedNeighborhood = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(neighborhood));
        using var envelope = SealProvider.Factory.CreateSigner(privateKey, passphrase ?? string.Empty);
        var signature = envelope.Sign(serializedNeighborhood);
        return new VertexBroadcastRequest
        {
            PublicKey = publicKey,
            SignedData = Convert.ToBase64String(signature!)
        };
    }

    public static async Task Main(string[] args)
    {
        string localAddress;
        string publicKey;
        string privateKey;
        string passphrase = PKey.Passphrase;

        string server = $"http://{args[1]}".Trim();
        string onionRoutingEndpoint = $"{server}/{Endpoints.OnionRoutingEndpoint}";
        string serverInfoEndpoint = $"{server}/{Endpoints.InfoEndpoint}";

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

        connection.On<RoutingRequest>(nameof(IEnigmaHub.RouteMessage), message =>
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

        var tokenResult = await connection.InvokeAsync<InvocationResult<string>>(nameof(IEnigmaHub.GenerateToken));
        HandleServerResponse(tokenResult, nameof(IEnigmaHub.GenerateToken));

        using var signature = SealProvider.Factory.CreateSigner(privateKey, passphrase);
        var encodedNonce = Convert.FromBase64String(tokenResult.Data!) ?? throw new Exception("Failed to base64 decode nonce.");
        var data = signature.Sign(encodedNonce) ?? throw new Exception("Nonce signature failed.");
        var authenticationResult = await connection.InvokeAsync<InvocationResult<bool>>(nameof(IEnigmaHub.Authenticate), new AuthenticationRequest
        {
            PublicKey = publicKey,
            Signature = Convert.ToBase64String(data)
        });
        HandleServerResponse(authenticationResult, nameof(IEnigmaHub.Authenticate));

        Console.WriteLine($"Authenticated.");

        var pullResult = await connection.InvokeAsync<InvocationResult<List<PendingMessage>>>(nameof(IEnigmaHub.Pull));
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
        var broadcastVertexRequest = CreateVertexBroadcast(localAddress, CertificateHelper.GetHexAddressFromPublicKey(serverPublicKey), publicKey, privateKey, passphrase);
        var broadcastResult = await connection.InvokeAsync<InvocationResult<bool>>(nameof(IEnigmaHub.Broadcast), broadcastVertexRequest);

        Console.WriteLine($"Broadcast result: {broadcastResult.Data}");

        while (args[0] == "1" && serverPublicKey != null)
        {
            var destinationPublicKey = PKey.PublicKey2;
            var destinationAddress = HashProvider.FromHexString(PKey.Address2);

            var onion = SealProvider.SealOnion(Encoding.UTF8.GetBytes(message), [destinationPublicKey, serverPublicKey], [PKey.Address1, PKey.Address2]);
            await connection.InvokeAsync(nameof(IEnigmaHub.RouteMessage), new RoutingRequest(onion!));

            Console.ReadLine();
        }

        Console.ReadLine();
        await connection.StopAsync();
    }
}
