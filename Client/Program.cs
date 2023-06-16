using System.Text;
using Enigma5.Crypto;
using Enigma5.Message;
using Microsoft.AspNetCore.SignalR.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        // certificate and private key for receiving data or
        // certificate and 2 public keys for sending data
        if (args.Length != 2 && args.Length != 3)
        {
            throw new ArgumentException("Wrong number of certificates and key.");
        }

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/OnionRouting")
            .Build();

        connection.On<string>("RouteMessage", message =>
        {
            Console.WriteLine($"Message: {message}");
        });

        connection.On<bool>("ValidateCertificate", status =>
        {
            Console.WriteLine($"Identity confirmation status: {status}");
        });

        await connection.StartAsync();
        await connection.InvokeAsync("ValidateCertificate", File.ReadAllText(args[0]));

        //while (connection.State == HubConnectionState.Connected)
        //{
        //Console.WriteLine("Enter a message (or 'exit' to quit):");
        var message = "Test";

        // if (message!.ToLower() == "exit")
        //     break;

        if (args.Length == 3)
        {
            var serverPublicKey = File.ReadAllText(args[1]);
            var destinationPublicKey = File.ReadAllText(args[2]);
            //var serverAddress = CertificateHelper.GetAddressFromPublicKey(serverPublicKey);
            var destinationAddress = CertificateHelper.GetAddressFromPublicKey(destinationPublicKey);

            var onion = OnionBuilder
                .Create()
                .SetMessageContent(Encoding.UTF8.GetBytes(message))
                .SetNextAddress(destinationAddress)
                .Seal(destinationPublicKey)
                .AddPeel()
                .SetNextAddress(destinationAddress)
                .Seal(serverPublicKey)
                .Build();
            await connection.InvokeAsync("RouteMessage", Convert.ToBase64String(onion.Content));
        }
        //}

        Console.ReadLine();

        await connection.StopAsync();
    }
}
