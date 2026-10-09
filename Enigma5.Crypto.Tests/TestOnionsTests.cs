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
using Enigma5.Tests.Base;

namespace Enigma5.Crypto.Tests;

// The onions that other tests send to a node: each must open with the keys it was made for.
public class TestOnionsTests
{
    private static (string? Next, byte[]? Content) Open(string onion, string privateKey)
    {
        using var unsealer = SealProvider.Factory.CreateUnsealer(privateKey, TestKeys.PublicKeyOf(privateKey));
        string? next = null;
        byte[]? content = null;
        unsealer.UnsealOnion(onion, ref next, ref content);
        return (next, content);
    }

    [Theory]
    [InlineData(TestOnions.ForClientThroughA1, "message 1")]
    [InlineData(TestOnions.ForClientThroughA2, "message 2")]
    [InlineData(TestOnions.ForClientThroughA3, "message 3")]
    public void An_onion_through_node_A_names_the_client_and_holds_the_content(string onion, string expectedContent)
    {
        var (next, content) = Open(onion, TestKeys.PlainPrivateKey1);

        Assert.Equal(TestKeys.Address3, next);
        Assert.Equal(expectedContent, Encoding.UTF8.GetString(content!));
    }

    [Fact]
    public void An_onion_through_A_then_B_names_B_at_A_and_the_client_at_B()
    {
        var (first, inner) = Open(TestOnions.ForClientThroughAThenB, TestKeys.PlainPrivateKey1);
        var (second, content) = Open(Convert.ToBase64String(inner!), TestKeys.PlainPrivateKey2);

        Assert.Equal(TestKeys.Address2, first);
        Assert.Equal(TestKeys.Address3, second);
        Assert.Equal("from A to B", Encoding.UTF8.GetString(content!));
    }

    [Fact]
    public void An_onion_through_B_then_A_names_A_at_B_and_the_client_at_A()
    {
        var (first, inner) = Open(TestOnions.ForClientThroughBThenA, TestKeys.PlainPrivateKey2);
        var (second, content) = Open(Convert.ToBase64String(inner!), TestKeys.PlainPrivateKey1);

        Assert.Equal(TestKeys.Address1, first);
        Assert.Equal(TestKeys.Address3, second);
        Assert.Equal("from B to A", Encoding.UTF8.GetString(content!));
    }

    [Fact]
    public void An_onion_for_node_B_cannot_be_opened_by_node_A()
    {
        Assert.Equal((null, null), Open(TestOnions.ForClientThroughB, TestKeys.PlainPrivateKey1));
        Assert.Equal(TestKeys.Address3, Open(TestOnions.ForClientThroughB, TestKeys.PlainPrivateKey2).Next);
    }

    [Fact]
    public void The_large_onion_is_within_the_size_a_node_accepts()
    {
        Assert.InRange(TestOnions.LargeForClientThroughA.Length, 15000, App.Common.Constants.MaxOnionSize);
        Assert.Equal(TestKeys.Address3, Open(TestOnions.LargeForClientThroughA, TestKeys.PlainPrivateKey1).Next);
    }
}
