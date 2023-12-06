using Enigma5.App.Common.Extensions;
using Microsoft.Extensions.Configuration;
using NetworkBridge;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var urls = configuration.GetPeers() ?? throw new Exception("Peers section not provided into configuration.");

var hubConnectionFactory = new HubConnectionFactory(configuration);
var connections = hubConnectionFactory.Create(urls);

connections.Start();
connections.StartAuthentication();


Console.ReadLine();
