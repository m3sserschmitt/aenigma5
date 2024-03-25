using Autofac;
using Autofac.Builder;
using Enigma5.App.Data;

namespace Enigma5.App.Tests;

public static class ContainerBuilderExtensions
{
    public static IRegistrationBuilder<Vertex, SimpleActivatorData, SingleRegistrationStyle> RegisterVertex(this ContainerBuilder containerBuilder)
    {
        return containerBuilder.Register((c, args) =>
        {
            var publicKey = args.Named<string>("publicKey");
            var privateKey = args.Named<byte[]>("privateKey");
            var passphrase = args.Named<string>("passphrase");
            var address = args.Named<string>("address");
            var neighbors = args.Named<List<string>>("neighbors");
            var hostname = args.Named<string>("hostname");

            return Vertex.Factory.Create(publicKey, privateKey, address, neighbors, passphrase, hostname == string.Empty ? null : hostname);
        });
    }
}
