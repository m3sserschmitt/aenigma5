using System.Text;
using Enigma5.App.Common.Contracts.Hubs;
using Enigma5.Crypto.DataProviders;
using Enigma5.Message;
using Microsoft.AspNetCore.SignalR.Client;
using Enigma5.App.Models;
using Enigma5.Crypto;
using System.Net.Http.Json;
using Enigma5.App.Common.Constants;

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

        connection.On<string>(nameof(IHub.RouteMessage), message =>
        {
            HandleMessage(message, privateKey, passphrase);
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
        await connection.InvokeAsync<string?>(nameof(IHub.GenerateToken))
        .ContinueWith(async response =>
        {
            var token = await response ?? throw new Exception("Token generation failed.");

            using var signature = Envelope.Factory.CreateSignature(Encoding.UTF8.GetBytes(privateKey), passphrase);
            var data = signature.Sign(Convert.FromBase64String(token));

            if (data != null)
            {
                await connection.InvokeAsync<bool>(nameof(IHub.Authenticate), new AuthenticationRequest
                {
                    PublicKey = publicKey,
                    Signature = Convert.ToBase64String(data),
                    SyncMessagesOnSuccess = true,
                    UpdateNetworkGraph = false
                }).ContinueWith(async response =>
                {
                    var authenticated = await response;
                    Console.WriteLine($"Authenticated: {authenticated}");
                });
            }
        });

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
            await connection.InvokeAsync(nameof(IHub.RouteMessage), Convert.ToBase64String(onion.Content));

            Console.ReadLine();
        }

        Console.ReadLine();
        await connection.StopAsync();
    }
}
