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
using Enigma5.App.Extensions;
using Enigma5.App.Hubs;
using Enigma5.Tests.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Enigma5.App.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    private const string Public = "http://127.0.0.1:8080";

    // The shipped rules: on the public endpoint the dashboard pages are closed, and so is the hub method TriggerBroadcast.
    private static IConfiguration Shipped(string httpEndpoint = Public, string hubEndpoint = Public) => TestConfiguration.Create(
        ("HttpBlacklists:0:Endpoint", httpEndpoint),
        ("HttpBlacklists:0:Items:0:Path", "/Dashboard"),
        ("HttpBlacklists:0:Items:0:Methods:0", "GET"),
        ("HttpBlacklists:0:Items:1:Path", "/_blazor"),
        ("HttpBlacklists:0:Items:1:Methods:0", "GET"),
        ("HttpBlacklists:0:Items:1:Methods:1", "POST"),
        ("HubBlacklists:0:Endpoint", hubEndpoint),
        ("HubBlacklists:0:Items:0:Methods:0", "TriggerBroadcast"));

    private static HttpContext Request(string method, string path, string localAddress = "127.0.0.1", int localPort = 8080)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Connection.LocalIpAddress = IPAddress.Parse(localAddress);
        context.Connection.LocalPort = localPort;
        return context;
    }

    private static HubInvocationContext HubCall(string methodName, string? localAddress = "127.0.0.1", int? localPort = 8080)
    {
        // The hub stores both values when a connection opens; a value it could not find out is stored as null.
        var items = new Dictionary<object, object?>
        {
            [Common.Constants.HubConnectionLocalIpKey] = localAddress is null ? null : IPAddress.Parse(localAddress),
            [Common.Constants.HubConnectionLocalPortKey] = localPort
        };
        var caller = Substitute.For<HubCallerContext>();
        caller.Items.Returns(items);
        caller.Features.Returns(new FeatureCollection());
        return new HubInvocationContext(caller, Substitute.For<IServiceProvider>(), Substitute.For<Hub>(), typeof(RoutingHub).GetMethod(methodName)!, []);
    }

    #region HTTP requests

    [Theory]
    [InlineData("GET", "/Dashboard")]
    [InlineData("get", "/Dashboard")]
    [InlineData("GET", "/dashboard")]
    [InlineData("GET", "/Dashboard/anything")]
    [InlineData("GET", "/_blazor")]
    [InlineData("POST", "/_blazor/negotiate")]
    public void A_request_named_by_a_rule_of_its_endpoint_is_refused(string method, string path)
    {
        Assert.False(Shipped().IsHttpRequestCallAuthorized(Request(method, path)));
    }

    [Theory]
    [InlineData("GET", "/Info")]
    [InlineData("POST", "/Share")]
    [InlineData("GET", "/DashboardX")]
    [InlineData("POST", "/Dashboard")]
    public void A_request_with_another_path_or_method_is_allowed(string method, string path)
    {
        Assert.True(Shipped().IsHttpRequestCallAuthorized(Request(method, path)));
    }

    [Fact]
    public void A_request_on_another_endpoint_is_allowed()
    {
        Assert.True(Shipped().IsHttpRequestCallAuthorized(Request("GET", "/Dashboard", localPort: 8081)));
        Assert.True(Shipped().IsHttpRequestCallAuthorized(Request("GET", "/Dashboard", localAddress: "10.0.0.1")));
    }

    [Fact]
    public void A_rule_for_all_addresses_applies_to_every_address_on_its_port()
    {
        var configuration = Shipped(httpEndpoint: "http://0.0.0.0:8080");

        Assert.False(configuration.IsHttpRequestCallAuthorized(Request("GET", "/Dashboard", localAddress: "10.0.0.1")));
        Assert.True(configuration.IsHttpRequestCallAuthorized(Request("GET", "/Dashboard", localAddress: "10.0.0.1", localPort: 8081)));
    }

    [Fact]
    public void A_rule_whose_endpoint_has_a_host_name_never_applies()
    {
        var configuration = Shipped(httpEndpoint: "http://localhost:8080");

        Assert.True(configuration.IsHttpRequestCallAuthorized(Request("GET", "/Dashboard")));
    }

    [Fact]
    public void Without_rules_every_request_is_allowed()
    {
        Assert.True(TestConfiguration.Create().IsHttpRequestCallAuthorized(Request("GET", "/Dashboard")));
    }

    [Fact]
    public void A_request_whose_local_address_is_unknown_is_refused_when_rules_exist()
    {
        var request = Request("GET", "/Info");
        request.Connection.LocalIpAddress = null;

        Assert.False(Shipped().IsHttpRequestCallAuthorized(request));
    }

    // Known limit: rules for the same endpoint in a second list have no effect.
    [Fact]
    public void Only_the_first_list_that_matches_the_endpoint_is_used()
    {
        var configuration = TestConfiguration.Create(
            ("HttpBlacklists:0:Endpoint", Public),
            ("HttpBlacklists:0:Items:0:Path", "/Dashboard"),
            ("HttpBlacklists:0:Items:0:Methods:0", "GET"),
            ("HttpBlacklists:1:Endpoint", Public),
            ("HttpBlacklists:1:Items:0:Path", "/Info"),
            ("HttpBlacklists:1:Items:0:Methods:0", "GET"));

        Assert.False(configuration.IsHttpRequestCallAuthorized(Request("GET", "/Dashboard")));
        Assert.True(configuration.IsHttpRequestCallAuthorized(Request("GET", "/Info")));
    }

    #endregion

    #region Hub methods

    [Fact]
    public void A_hub_method_named_by_a_rule_of_its_endpoint_is_refused()
    {
        Assert.False(Shipped().IsHubMethodCallAuthorized(HubCall(nameof(RoutingHub.TriggerBroadcast))));
    }

    [Fact]
    public void Another_hub_method_is_allowed()
    {
        Assert.True(Shipped().IsHubMethodCallAuthorized(HubCall(nameof(RoutingHub.GenerateToken))));
    }

    [Fact]
    public void A_hub_method_on_another_endpoint_is_allowed()
    {
        Assert.True(Shipped().IsHubMethodCallAuthorized(HubCall(nameof(RoutingHub.TriggerBroadcast), localPort: 8081)));
    }

    [Fact]
    public void A_hub_call_whose_local_address_or_port_is_unknown_is_refused_when_rules_exist()
    {
        Assert.False(Shipped().IsHubMethodCallAuthorized(HubCall(nameof(RoutingHub.GenerateToken), localAddress: null)));
        Assert.False(Shipped().IsHubMethodCallAuthorized(HubCall(nameof(RoutingHub.GenerateToken), localPort: null)));
    }

    [Fact]
    public void Without_rules_every_hub_method_is_allowed()
    {
        Assert.True(TestConfiguration.Create().IsHubMethodCallAuthorized(HubCall(nameof(RoutingHub.TriggerBroadcast), localAddress: null, localPort: null)));
    }

    #endregion

    #region Warnings

    [Fact]
    public void No_warning_is_logged_for_rules_whose_endpoints_have_an_ip_address()
    {
        var logger = new CapturingLogger<ConfigurationExtensionsTests>();

        Shipped().WarnAboutBlacklistsThatNeverMatch(logger);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void A_warning_names_each_rule_list_whose_endpoint_can_never_match()
    {
        var logger = new CapturingLogger<ConfigurationExtensionsTests>();

        Shipped(httpEndpoint: "http://localhost:8080", hubEndpoint: "nonsense").WarnAboutBlacklistsThatNeverMatch(logger);

        Assert.Equal(2, logger.Warnings.Count);
        Assert.Contains(logger.Warnings, entry => Equals(entry.Properties["Section"], "HttpBlacklists") && Equals(entry.Properties["Endpoint"], "http://localhost:8080"));
        Assert.Contains(logger.Warnings, entry => Equals(entry.Properties["Section"], "HubBlacklists") && Equals(entry.Properties["Endpoint"], "nonsense"));
    }

    [Fact]
    public void A_warning_names_each_setting_whose_value_cannot_be_read()
    {
        var logger = new CapturingLogger<ConfigurationExtensionsTests>();
        var configuration = TestConfiguration.Create(("VertexLifetime", "half an hour"), ("KeySource", "Foo"), ("SharedDataMaxSize", "1024"));

        configuration.WarnAboutInvalidSettings(logger);

        Assert.Equal(2, logger.Warnings.Count);
        var warning = Assert.Single(logger.Warnings, entry => Equals(entry.Properties["Setting"], "VertexLifetime"));
        Assert.Equal("half an hour", warning.Properties["Value"]);
        Assert.Equal("00:30:00", warning.Properties["Default"]);
    }

    [Fact]
    public void No_warning_is_logged_when_every_setting_can_be_read()
    {
        var logger = new CapturingLogger<ConfigurationExtensionsTests>();

        Shipped().WarnAboutInvalidSettings(logger);

        Assert.Empty(logger.Entries);
    }

    #endregion
}
