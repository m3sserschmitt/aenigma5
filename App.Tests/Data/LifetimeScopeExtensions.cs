using Autofac;
using Enigma5.App.Data;
using Enigma5.App.Security;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.App.Tests;

public static class LifetimeScopeExtensions
{
    public static Vertex ResolveVertex(this ILifetimeScope scope, string publicKey, string privateKey, string address, List<string> neighbors, string? passphrase = null, string? hostname = null)
    {
        return scope.Resolve<Vertex>(
            new NamedParameter("publicKey", publicKey),
            new NamedParameter("privateKey", privateKey),
            new NamedParameter("passphrase", passphrase ?? string.Empty),
            new NamedParameter("address", address),
            new NamedParameter("neighbors", neighbors),
            new NamedParameter("hostname", hostname ?? string.Empty));
    }

    public static Vertex ResolveLocalVertex(this ILifetimeScope scope, List<string> neighbors, string? hostname = null)
    {
        var certificateManager = scope.Resolve<CertificateManager>();

        return scope.ResolveVertex(certificateManager.PublicKey, certificateManager.PrivateKey, certificateManager.Address, neighbors, hostname);
    }

    public static Vertex ResolveAdjacentVertex(this ILifetimeScope scope, List<string> neighbors, string? hostname = null)
    {
        var certificateManager = scope.Resolve<CertificateManager>();

        var l = new List<string>() { certificateManager.Address };
        l.AddRange(neighbors);

        return scope.ResolveVertex(PKey.PublicKey1, PKey.PrivateKey1, PKey.Address1, l, PKey.Passphrase, hostname);
    }

    public static Vertex ResolveNonAdjacentVertex(this ILifetimeScope scope, List<string> neighbors, string? hostname = null)
    {
        return scope.ResolveVertex(PKey.PublicKey1, PKey.PrivateKey1, PKey.Address1, neighbors, PKey.Passphrase, hostname);
    }
}
