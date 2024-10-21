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
using Autofac;
using Enigma5.App.Data;
using Enigma5.Security.Contracts;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.App.Tests;

public static class LifetimeScopeExtensions
{
    public static Vertex ResolveVertex(this ILifetimeScope scope, string publicKey, byte[] privateKey, string address, List<string> neighbors, string? passphrase = null, string? hostname = null)
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
        var certificateManager = scope.Resolve<ICertificateManager>();

        return scope.ResolveVertex(certificateManager.PublicKey, certificateManager.PrivateKey, certificateManager.Address, neighbors, null, hostname);
    }

    public static Vertex ResolveLocalVertex(this ILifetimeScope scope, string? hostname = null)
    => scope.ResolveLocalVertex([], hostname);

    public static Vertex ResolveAdjacentVertex(this ILifetimeScope scope, List<string> neighbors, string? hostname = null)
    {
        var certificateManager = scope.Resolve<ICertificateManager>();

        var l = new List<string>() { certificateManager.Address };
        l.AddRange(neighbors);

        return scope.ResolveVertex(PKey.PublicKey1, Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Address1, l, PKey.Passphrase, hostname);
    }

    public static Vertex ResolveAdjacentVertex(this ILifetimeScope scope, string? hostname = null)
    => scope.ResolveAdjacentVertex([], hostname);

    public static Vertex ResolveNonAdjacentVertex(this ILifetimeScope scope, List<string> neighbors, string? hostname = null)
    => scope.ResolveVertex(PKey.PublicKey1, Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Address1, neighbors, PKey.Passphrase, hostname);

    public static Vertex ResolveNonAdjacentVertex(this ILifetimeScope scope, string? hostname = null)
    => scope.ResolveNonAdjacentVertex([], hostname);
}
