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

using Autofac;
using Enigma5.App.Data;
using Enigma5.Security.Contracts;
using Enigma5.Crypto.DataProviders;
using Enigma5.Crypto;
using Enigma5.App.Common.Extensions;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;
namespace Enigma5.App.Tests.Helpers;

[ExcludeFromCodeCoverage]
public static class ContainerExtensions
{
    public static Vertex ResolveVertex(this IContainer scope, string publicKey, string privateKey, string passphrase, HashSet<string> neighbors, string? hostname)
    {
        var signer = SealProvider.Factory.CreateSigner(privateKey, passphrase);
        hostname ??= string.Empty;
        return scope.Resolve<Vertex>(
            new NamedParameter("publicKey", publicKey),
            new NamedParameter("signer", signer),
            new NamedParameter("neighbors", neighbors),
            new NamedParameter("hostname", hostname));
    }

    public static Vertex ResolveLocalVertex(this IContainer scope, HashSet<string> neighbors)
    {
        var config = scope.Resolve<IConfiguration>();
        return scope.ResolveVertex(PKey.PublicKey3, PKey.PrivateKey3, string.Empty, neighbors, config.GetHostname());
    }
    public static Vertex ResolveLocalVertex(this IContainer scope)
    => scope.ResolveLocalVertex([]);

    public static Vertex ResolveAdjacentVertex(this IContainer scope, HashSet<string> neighbors, string? hostname = "adjacent-hostname")
    {
        var certificateManager = scope.Resolve<ICertificateManager>();
        var allNeighbors = new HashSet<string>() { certificateManager.Address };
        allNeighbors.UnionWith(neighbors);
        return scope.ResolveVertex(PKey.PublicKey1, PKey.PrivateKey1, PKey.Passphrase, allNeighbors, hostname);
    }

    public static Vertex ResolveAdjacentVertex(this IContainer scope, string? hostname = "adjacent-hostname")
    => scope.ResolveAdjacentVertex([], hostname);

    public static Vertex ResolveNonAdjacentVertex(this IContainer scope, HashSet<string> neighbors, string? hostname = "adjacent-hostname")
    => scope.ResolveVertex(PKey.PublicKey1, PKey.PrivateKey1, PKey.Passphrase, neighbors, hostname);

    public static Vertex ResolveNonAdjacentVertex(this IContainer scope, string? hostname = "adjacent-hostname")
    => scope.ResolveNonAdjacentVertex([], hostname);
}
