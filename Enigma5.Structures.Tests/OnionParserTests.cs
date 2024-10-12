/*
    Aenigma - Onion Routing based messaging application
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
using Enigma5.Structures.Contracts;
using Enigma5.Structures.DataProviders.Contracts;
using Enigma5.Structures.DataProviders;
using Enigma5.Crypto.DataProviders;
using Xunit;
using System.Text;

namespace Enigma5.Structures.Tests;

public class OnionParserTests
{
    private readonly IContainer _container;

    public OnionParserTests()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.Register(c => OnionBuilder.Create()).As<ISetMessageContent>();
        containerBuilder.RegisterType<TestOnion>().As<ITestOnion>();
        containerBuilder.Register(c => new TestOnionPeel(c.Resolve<ITestOnion>()));

        _container = containerBuilder.Build();
    }

    [Fact]
    public void OnionParser_ShouldParse()
    {
        // Arrange
        using var onionParser = OnionParser.Factory.Create(Encoding.UTF8.GetBytes(PKey.PrivateKey2), PKey.Passphrase);
        using var scope = _container.BeginLifetimeScope();
        var onion = scope.Resolve<ITestOnion>();

        // Act
        var result = onionParser.Parse(onion);

        // Assert
        Assert.True(result);
        Assert.Equal(onion.ExpectedNextAddress, onionParser.NextAddress);
        Assert.Equal(onion.ExpectedContent, onionParser.Content);
    }

    [Fact]
    public void OnionParser_ShouldRemovePeel()
    {
        // Arrange
        using var onionParser = OnionParser.Factory.Create(Encoding.UTF8.GetBytes(PKey.ServerPrivateKey), PKey.Passphrase);
        using var scope = _container.BeginLifetimeScope();
        var onion = scope.Resolve<TestOnionPeel>();

        // Act
        var result = onionParser.Parse(onion);

        // Assert
        Assert.True(result);
        Assert.Equal(onion.ExpectedNextAddress, onionParser.NextAddress);
        Assert.Equal(onion.ExpectedContent, onionParser.Content);
    }
}
