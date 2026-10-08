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

using System.Net;
using Enigma5.App.Common.Extensions;
using Enigma5.Tests.Base;

namespace Enigma5.App.Common.Tests.Extensions;

public class StringExtensionsTests
{
    private const string Guid = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    private const string Onion = "scsdxdhyp5ljpruh65tzkkopwd2fcdn7tyq6a2fndkh2323xsbsgn3id.onion";

    #region Guids and tags

    [Fact]
    public void IsValidGuid_accepts_the_standard_form()
    {
        Assert.True(Guid.IsValidGuid());
    }

    [Fact]
    public void IsValidGuid_accepts_upper_case()
    {
        Assert.True(Guid.ToUpperInvariant().IsValidGuid());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("3f2504e0-4f89-11d3-9a0c")]
    [InlineData("../3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
    public void IsValidGuid_refuses_anything_else(string? value)
    {
        Assert.False(value.IsValidGuid());
    }

    [Fact]
    public void NormalizeGuid_returns_the_lower_case_form()
    {
        Assert.Equal(Guid, Guid.ToUpperInvariant().NormalizeGuid());
    }

    [Fact]
    public void NormalizeGuid_returns_null_for_a_value_that_is_not_a_guid()
    {
        Assert.Null("abc".NormalizeGuid());
    }

    [Fact]
    public void NormalizeTag_follows_the_rules_for_guids()
    {
        Assert.Equal(Guid, Guid.ToUpperInvariant().NormalizeTag());
        Assert.Null("../etc/passwd".NormalizeTag());
        Assert.Null(((string?)null).NormalizeTag());
    }

    #endregion

    #region Addresses

    [Fact]
    public void IsValidAddress_accepts_64_lower_case_hex_characters()
    {
        Assert.True(TestKeys.Address1.IsValidAddress());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cbff2e12")]
    [InlineData("CBFF2E12FB1F752CB17185F080F2B40301165A1051531CC0614E495EE2620EF9")]
    [InlineData("zbff2e12fb1f752cb17185f080f2b40301165a1051531cc0614e495ee2620ef9")]
    [InlineData("cbff2e12fb1f752cb17185f080f2b40301165a1051531cc0614e495ee2620ef9a")]
    public void IsValidAddress_refuses_other_values(string? value)
    {
        Assert.False(value.IsValidAddress());
    }

    [Fact]
    public void NormalizeAddress_trims_and_lowers_the_value()
    {
        Assert.Equal(TestKeys.Address1, $"  {TestKeys.Address1.ToUpperInvariant()} ".NormalizeAddress());
    }

    [Fact]
    public void NormalizeAddress_returns_null_for_a_value_that_is_not_an_address()
    {
        Assert.Null("cbff2e12".NormalizeAddress());
        Assert.Null(((string?)null).NormalizeAddress());
    }

    #endregion

    #region Keys and base64

    [Fact]
    public void IsValidPublicKey_accepts_a_public_key_in_PEM_form()
    {
        Assert.True(TestKeys.PublicKey1.IsValidPublicKey());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a key")]
    [InlineData("-----BEGIN PUBLIC KEY-----\n!!!!\n-----END PUBLIC KEY-----")]
    public void IsValidPublicKey_refuses_other_values(string? value)
    {
        Assert.False(value.IsValidPublicKey());
    }

    [Fact]
    public void IsValidPublicKey_refuses_a_private_key()
    {
        Assert.False(TestKeys.PrivateKey1.IsValidPublicKey());
    }

    [Fact]
    public void IsValidPrivateKey_accepts_plain_and_encrypted_private_keys()
    {
        Assert.True(TestKeys.PrivateKey3.IsValidPrivateKey());
        Assert.True(TestKeys.PrivateKey1.IsValidPrivateKey());
    }

    [Fact]
    public void IsValidPrivateKey_refuses_a_public_key()
    {
        Assert.False(TestKeys.PublicKey1.IsValidPrivateKey());
    }

    [Fact]
    public void GetPublicKeyBase64_returns_the_key_without_header_and_line_breaks()
    {
        var content = TestKeys.PublicKey1.GetPublicKeyBase64();

        Assert.NotNull(content);
        Assert.DoesNotContain("-----", content);
        Assert.DoesNotContain("\n", content);
        Assert.True(content.IsValidBase64());
    }

    [Theory]
    [InlineData("dGVzdC1zdHJpbmc=", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("not base64!", false)]
    public void IsValidBase64_tells_base64_from_other_text(string? value, bool expected)
    {
        Assert.Equal(expected, value.IsValidBase64());
    }

    #endregion

    #region Onion addresses and URLs

    [Theory]
    [InlineData("http://" + Onion, true)]
    [InlineData("http://" + Onion + "/", true)]
    [InlineData("https://" + Onion + ":8080/path", true)]
    [InlineData(Onion, false)]
    [InlineData("http://example.com", false)]
    [InlineData("http://tooshort.onion", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidOnionUrl_needs_an_absolute_url_with_an_onion_host(string? value, bool expected)
    {
        Assert.Equal(expected, value.IsValidOnionUrl());
    }

    [Theory]
    [InlineData(Onion, true)]
    [InlineData("expyuzz4wqqyqhjn.onion", true)]
    [InlineData("example.com", false)]
    [InlineData("abc.onion", false)]
    [InlineData(null, false)]
    public void IsValidOnionAddress_accepts_16_and_56_character_names(string? value, bool expected)
    {
        Assert.Equal(expected, value.IsValidOnionAddress());
    }

    #endregion

    #region MatchUrl

    [Theory]
    [InlineData("http://127.0.0.1:8080", "127.0.0.1", 8080, true)]
    [InlineData("http://127.0.0.1:8080/", "127.0.0.1", 8080, true)]
    [InlineData("http://127.0.0.1:8080", "127.0.0.1", 8081, false)]
    [InlineData("http://127.0.0.1:8080", "10.0.0.1", 8080, false)]
    [InlineData("http://0.0.0.0:8080", "10.0.0.1", 8080, true)]
    [InlineData("http://0.0.0.0:8080", "10.0.0.1", 8081, false)]
    [InlineData("http://127.0.0.1:8080", "::ffff:127.0.0.1", 8080, true)]
    public void MatchUrl_compares_address_and_port(string url, string address, int port, bool expected)
    {
        Assert.Equal(expected, url.MatchUrl(IPAddress.Parse(address), port));
    }

    [Theory]
    [InlineData("http://localhost:8080")]
    [InlineData("127.0.0.1:8080")]
    [InlineData("nonsense")]
    [InlineData(null)]
    public void MatchUrl_never_matches_a_url_without_an_ip_address(string? url)
    {
        Assert.False(url.MatchUrl(IPAddress.Loopback, 8080));
    }

    #endregion
}
