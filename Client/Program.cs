using System.Text;
using Enigma5.Crypto;
using Enigma5.Crypto.DataProviders;
using Enigma5.Message;
using Microsoft.AspNetCore.SignalR.Client;

namespace Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        string publicKey;
        string privateKey;
        string passphrase = PKey.Passphrase;

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
            .WithUrl("http://localhost:5000/OnionRouting")
            .Build();

        connection.On<string>("RouteMessage", message =>
        {
            Console.WriteLine($"Message received");
            var decodedData = Convert.FromBase64String(message);

            using var onionParser = OnionParser.Factory.Create(privateKey, passphrase);

            if (onionParser.Parse(new Onion { Content = decodedData }))
            {
                Console.WriteLine($"Message: {Encoding.UTF8.GetString(onionParser.Content!, 0, onionParser.Content!.Length)}");
            }
            else
            {
                Console.WriteLine("There was an error on decrypting the message");
            }
        });

        connection.On<string?>("GenerateToken", async token =>
        {
            Console.WriteLine($"Token Generated: {token}");

            if (token != null)
            {
                using var signature = Envelope.Factory.CreateSignature(privateKey, passphrase);
                var data = signature.Sign(Convert.FromBase64String(token));

                if (data != null)
                {
                    await connection.InvokeAsync("Authenticate", publicKey, Convert.ToBase64String(data));
                }
            }
        });

        connection.On<bool>("Authenticate", authenticated =>
        {
            Console.WriteLine($"Authenticated: {authenticated}");
        });

        await connection.StartAsync();
        await connection.InvokeAsync("GenerateToken");

        var message = "Test";

        while (args[0] == "1")
        {
            var serverPublicKey = PKey.ServerPublicKey;
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
            await connection.InvokeAsync("RouteMessage", Convert.ToBase64String(onion.Content));

            Console.ReadLine();
        }

        Console.ReadLine();
        await connection.StopAsync();
    }
}
