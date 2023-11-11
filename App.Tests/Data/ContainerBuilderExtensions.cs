using Autofac;
using Autofac.Builder;
using Enigma5.App.Data;
using Enigma5.App.Security;

namespace App.Tests;

public static class ContainerBuilderExtensions
{
    public static IRegistrationBuilder<Vertex, SimpleActivatorData, SingleRegistrationStyle> RegisterVertex(this ContainerBuilder containerBuilder)
    {
        return containerBuilder.Register((c, args) =>
        {
            var publicKey = args.Named<string>("publicKey");
            var privateKey = args.Named<string>("privateKey");
            var passphrase = args.Named<string>("passphrase");
            var address = args.Named<string>("address");
            var neighbors = args.Named<List<string>>("neighbors");
            var hostname = args.Named<string>("hostname");

            return Vertex.Create(publicKey, privateKey, address, neighbors, passphrase, hostname == string.Empty ? null : hostname);
        });
    }

    public static IRegistrationBuilder<CertificateManager, SimpleActivatorData, SingleRegistrationStyle> RegisterCertificateManager(this ContainerBuilder containerBuilder)
    {
        return containerBuilder.Register(_ =>
        {
            return new CertificateManager();
        });
    }
}
