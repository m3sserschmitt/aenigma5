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
using System.Net.Http.Json;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests.Settings;

// A node with values that cannot be read, and with blacklist entries whose endpoint is not an IP address.
public class InvalidSettingsFixture : NodeAFixture
{
    protected override (string Key, string Value)[] Settings =>
    [
        ("SharedDataMaxSize", "plenty"),
        ("VertexLifetime", "half an hour"),
        ("HttpBlacklists:1:Endpoint", "http://localhost:5000"),
        ("HttpBlacklists:1:Items:0:Path", "/Dashboard"),
        ("HttpBlacklists:1:Items:0:Methods:0", "GET"),
        ("HubBlacklists:1:Endpoint", "public"),
        ("HubBlacklists:1:Items:0:Methods:0", "TriggerBroadcast")
    ];
}

public class InvalidSettingsTests(InvalidSettingsFixture fixture) : IClassFixture<InvalidSettingsFixture>
{
    private readonly NodeProcess _node = fixture.Node;

    [Fact]
    public async Task The_node_starts_and_answers()
    {
        Assert.Equal(HttpStatusCode.OK, (await _node.Public.GetAsync("/Info")).StatusCode);
    }

    [Theory]
    [InlineData("The setting SharedDataMaxSize has the value plenty, which cannot be read. The default 16384 is used instead.")]
    [InlineData("The setting VertexLifetime has the value half an hour, which cannot be read. The default 00:30:00 is used instead.")]
    [InlineData("The HttpBlacklists entry with endpoint http://localhost:5000 never applies")]
    [InlineData("The HubBlacklists entry with endpoint public never applies")]
    public void Each_bad_value_is_named_in_a_warning(string warning)
    {
        var line = Assert.Single(_node.Log.Split('\n'), line => line.Contains(warning));
        Assert.Contains("[WRN]", line);
    }

    [Fact]
    public void Good_values_give_no_such_warning()
    {
        Assert.DoesNotContain("SharedFileMaxSize", _node.Log);
        Assert.DoesNotContain(_node.PublicUrl + " never applies", _node.Log);
    }

    [Fact]
    public async Task The_default_is_used_in_place_of_a_value_that_cannot_be_read()
    {
        var withinTheDefault = await _node.Public.PostAsJsonAsync("/Share", new { publicKey = TestKeys.PublicKey3, signedData = TestSignedData.SharedPayloadSignedWithKey3, accessCount = 1 });
        var aboveTheDefault = await _node.Public.PostAsJsonAsync("/Share", new { publicKey = TestKeys.PublicKey3, signedData = new string('A', 16384), accessCount = 1 });

        Assert.Equal(HttpStatusCode.OK, withinTheDefault.StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, aboveTheDefault.StatusCode);
    }
}
