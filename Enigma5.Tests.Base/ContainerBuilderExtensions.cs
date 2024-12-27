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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Autofac;
using Autofac.Builder;
using Enigma5.App.Data;
using Enigma5.Crypto.Contracts;
using Microsoft.Extensions.Configuration;

namespace Enigma5.Tests.Base;

[ExcludeFromCodeCoverage]
public static class ContainerBuilderExtensions
{
    public static IRegistrationBuilder<Vertex, SimpleActivatorData, SingleRegistrationStyle> RegisterVertex(this ContainerBuilder containerBuilder)
    {
        return containerBuilder.Register((c, args) =>
        {
            var publicKey = args.Named<string>("publicKey");
            var signer = args.Named<IEnvelopeSigner>("signer");
            var neighbors = args.Named<HashSet<string>>("neighbors");
            var hostname = args.Named<string>("hostname");

            return Vertex.Factory.Create(publicKey, signer, neighbors, string.IsNullOrWhiteSpace(hostname) ? null : hostname)!;
        });
    }

    public static void SetupConfiguration(this ContainerBuilder container)
    {
        var jsonString = JsonSerializer.Serialize(new {
            Hostname = "http://localhost"
        });

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        container.RegisterInstance(config).As<IConfiguration>().SingleInstance();
    }
}
