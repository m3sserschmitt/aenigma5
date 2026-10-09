/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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
using Enigma5.Crypto;
using Enigma5.Security.Contracts;
using Enigma5.Structures.Tests.TestData;
using Enigma5.Tests.Base;
using NSubstitute;

namespace Enigma5.Structures.Tests;

public class OnionParserTests
{
    [Theory]
    [ClassData(typeof(ParserData))]
    public async Task ParseAsync_unseals_one_layer_of_a_stored_onion(string onion, string key, string? passphrase, bool expectedResult, string? expectedNext, byte[]? expectedContent)
    {
        var certificateManager = Substitute.For<ICertificateManager>();
        certificateManager.CreateUnsealerAsync().Returns(_ => SealProvider.Factory.CreateUnsealer(
            key, TestKeys.PublicKeyOf(key), passphrase is null ? null : Encoding.UTF8.GetBytes(passphrase + "\0")));
        var parser = new OnionParser(certificateManager);

        var result = await parser.ParseAsync(onion);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedNext, parser.NextAddress);
        Assert.Equal(expectedContent, parser.Content);
    }

    [Fact]
    public async Task ParseAsync_returns_false_when_no_unsealer_can_be_made()
    {
        var certificateManager = Substitute.For<ICertificateManager>();
        certificateManager.CreateUnsealerAsync().Returns<Task<Enigma5.Crypto.Contracts.IEnvelopeUnsealer>>(_ => throw new InvalidOperationException("locked"));
        var parser = new OnionParser(certificateManager);

        Assert.False(await parser.ParseAsync("AAEC"));
        Assert.Null(parser.NextAddress);
        Assert.Null(parser.Content);
    }

    [Fact]
    public async Task A_failed_parse_keeps_the_result_of_the_one_before()
    {
        var row = new ParserData().First();
        var (onion, key, passphrase) = ((string)row[0]!, (string)row[1]!, (string?)row[2]);
        var certificateManager = Substitute.For<ICertificateManager>();
        certificateManager.CreateUnsealerAsync().Returns(_ => SealProvider.Factory.CreateUnsealer(
            key, TestKeys.PublicKeyOf(key), passphrase is null ? null : Encoding.UTF8.GetBytes(passphrase + "\0")));
        var parser = new OnionParser(certificateManager);

        Assert.True(await parser.ParseAsync(onion));
        Assert.False(await parser.ParseAsync("not an onion"));

        // The filter that uses the parser reads these values only after a successful parse.
        Assert.Equal((string?)row[4], parser.NextAddress);
    }
}
