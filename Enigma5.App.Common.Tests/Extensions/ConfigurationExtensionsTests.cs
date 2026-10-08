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

using Enigma5.App.Common.Enums;
using Enigma5.App.Common.Extensions;
using Enigma5.Tests.Base;

namespace Enigma5.App.Common.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    private const string Tag = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    #region Defaults

    [Fact]
    public void Settings_that_are_missing_take_the_defaults_of_Constants()
    {
        var configuration = TestConfiguration.Create();

        Assert.Equal(Constants.DefaultMessageRetentionPeriod, configuration.GetMessageRetentionPeriod());
        Assert.Equal(Constants.DefaultSentMessageRetentionPeriod, configuration.GetSentMessageRetentionPeriod());
        Assert.Equal(Constants.DefaultSharedDataRetentionPeriod, configuration.GetSharedDataRetentionPeriod());
        Assert.Equal(Constants.DefaultFilesRetentionPeriod, configuration.GetFilesRetentionPeriod());
        Assert.Equal(Constants.DefaultVertexLifetime, configuration.GetVertexLifetime());
        Assert.Equal(Constants.DefaultUnlistedVertexGracePeriod, configuration.GetUnlistedVertexGracePeriod());
        Assert.Equal(Constants.DefaultDbProvider, configuration.GetDbProvider());
        Assert.Equal(Constants.DefaultKeySource, configuration.GetKeySource());
        Assert.Equal(Constants.DefaultPassphraseSource, configuration.GetPassphraseSource());
        Assert.Equal(Constants.DefaultPassphrasePersistence, configuration.GetPassphrasePersistence());
        Assert.Equal(Constants.DefaultSharedDataMaxSize, configuration.GetSharedDataMaxSize());
        Assert.Equal(Constants.DefaultSharedFileMaxSize, configuration.GetSharedFileMaxSize());
        Assert.Equal(Constants.DefaultDelayBetweenConnectionRetries, configuration.GetDelayBetweenConnectionRetries());
        Assert.Equal(Constants.DefaultDbConnectionString, configuration.GetDatabaseConnectionString());
        Assert.Equal(Constants.DefaultPrivateKeyPath, configuration.GetPrivateKeyPath());
        Assert.Equal(Constants.DefaultPublicKeyPath, configuration.GetPublicKeyPath());
        Assert.Equal(Constants.DefaultWebContentDirectory, configuration.GetWebContentDirectory());
        Assert.Equal(Constants.DefaultSocks5Proxy, configuration.GetSocks5Proxy());
    }

    [Fact]
    public void Settings_without_a_default_are_null_when_missing()
    {
        var configuration = TestConfiguration.Create();

        Assert.Null(configuration.GetHostname());
        Assert.Null(configuration.GetOnionService());
        Assert.Null(configuration.GetHttpEndpoint());
        Assert.Null(configuration.GetControlHttpEndpoint());
        Assert.Null(configuration.GetPassphraseKeyPath());
        Assert.Null(configuration.GetAzureVaultUrl());
    }

    [Fact]
    public void A_setting_that_is_empty_or_blank_counts_as_missing()
    {
        var configuration = TestConfiguration.Create(("VertexLifetime", "  "), ("Hostname", ""), ("PrivateKeyPath", " "));

        Assert.Equal(Constants.DefaultVertexLifetime, configuration.GetVertexLifetime());
        Assert.Null(configuration.GetHostname());
        Assert.Equal(Constants.DefaultPrivateKeyPath, configuration.GetPrivateKeyPath());
        Assert.Empty(configuration.GetInvalidSettings());
    }

    #endregion

    #region Values that can be read

    [Fact]
    public void Time_spans_are_read()
    {
        var configuration = TestConfiguration.Create(
            ("MessageRetentionPeriod", "07.00:00:00"), ("VertexLifetime", "00:10:00"), ("UnlistedVertexGracePeriod", "00:00:30"));

        Assert.Equal(TimeSpan.FromDays(7), configuration.GetMessageRetentionPeriod());
        Assert.Equal(TimeSpan.FromMinutes(10), configuration.GetVertexLifetime());
        Assert.Equal(TimeSpan.FromSeconds(30), configuration.GetUnlistedVertexGracePeriod());
    }

    [Fact]
    public void Numbers_are_read()
    {
        var configuration = TestConfiguration.Create(
            ("SharedDataMaxSize", "1024"), ("SharedFileMaxSize", "2048"), ("Network:DelayBetweenConnectionRetries", "500"));

        Assert.Equal(1024, configuration.GetSharedDataMaxSize());
        Assert.Equal(2048, configuration.GetSharedFileMaxSize());
        Assert.Equal(500, configuration.GetDelayBetweenConnectionRetries());
    }

    [Theory]
    [InlineData("Ephemeral")]
    [InlineData("ephemeral")]
    [InlineData("EPHEMERAL")]
    public void Choices_are_read_without_regard_to_case(string value)
    {
        var configuration = TestConfiguration.Create(("PassphrasePersistence", value));

        Assert.Equal(PassphrasePersistence.Ephemeral, configuration.GetPassphrasePersistence());
    }

    #endregion

    #region Values that cannot be read

    [Theory]
    [InlineData("MessageRetentionPeriod", "nonsense", "14.00:00:00")]
    [InlineData("SentMessageRetentionPeriod", "soon", "00:00:00")]
    [InlineData("SharedDataRetentionPeriod", "two weeks", "14.00:00:00")]
    [InlineData("FilesRetentionPeriod", "3 days", "3.00:00:00")]
    [InlineData("VertexLifetime", "half an hour", "00:30:00")]
    [InlineData("UnlistedVertexGracePeriod", "x", "00:06:00")]
    [InlineData("DbProvider", "5", "Sqlite")]
    [InlineData("KeySource", "Foo", "File")]
    [InlineData("PassphraseSource", "Foo", "Dashboard")]
    [InlineData("PassphrasePersistence", "Foo", "Persistent")]
    [InlineData("SharedDataMaxSize", "big", "16384")]
    [InlineData("SharedFileMaxSize", "1.5", "67108864")]
    [InlineData("Network:DelayBetweenConnectionRetries", "abc", "3000")]
    public void A_value_that_cannot_be_read_is_reported_with_its_default(string key, string value, string expectedDefault)
    {
        var configuration = TestConfiguration.Create((key, value));

        var invalid = Assert.Single(configuration.GetInvalidSettings());
        Assert.Equal(new InvalidSetting(key, value, expectedDefault), invalid);
    }

    [Fact]
    public void A_value_that_cannot_be_read_is_replaced_by_the_default()
    {
        var configuration = TestConfiguration.Create(
            ("VertexLifetime", "half an hour"), ("KeySource", "Foo"), ("SharedDataMaxSize", "big"), ("PassphrasePersistence", "7"));

        Assert.Equal(Constants.DefaultVertexLifetime, configuration.GetVertexLifetime());
        Assert.Equal(Constants.DefaultKeySource, configuration.GetKeySource());
        Assert.Equal(Constants.DefaultSharedDataMaxSize, configuration.GetSharedDataMaxSize());
        Assert.Equal(Constants.DefaultPassphrasePersistence, configuration.GetPassphrasePersistence());
    }

    [Fact]
    public void GetInvalidSettings_is_empty_when_every_value_can_be_read()
    {
        var configuration = TestConfiguration.Create(
            ("VertexLifetime", "00:10:00"), ("KeySource", "File"), ("SharedDataMaxSize", "1024"), ("Hostname", "anything goes"));

        Assert.Empty(configuration.GetInvalidSettings());
    }

    [Fact]
    public void GetInvalidSettings_reports_every_value_that_cannot_be_read()
    {
        var configuration = TestConfiguration.Create(
            ("VertexLifetime", "x"), ("KeySource", "y"), ("SharedDataMaxSize", "z"), ("MessageRetentionPeriod", "07.00:00:00"));

        Assert.Equal(["KeySource", "SharedDataMaxSize", "VertexLifetime"], configuration.GetInvalidSettings().Select(item => item.Key).Order());
    }

    #endregion

    #region Endpoints and URLs

    [Fact]
    public void Endpoints_and_the_hostname_lose_spaces_and_slashes_at_their_ends()
    {
        var configuration = TestConfiguration.Create(
            ("Hostname", " https://node.example.com/ "),
            ("Kestrel:EndPoints:Http:Url", "http://127.0.0.1:8080/"),
            ("Kestrel:EndPoints:HttpControl:Url", "http://127.0.0.1:8081/"));

        Assert.Equal("https://node.example.com", configuration.GetHostname());
        Assert.Equal("http://127.0.0.1:8080", configuration.GetHttpEndpoint());
        Assert.Equal("http://127.0.0.1:8081", configuration.GetControlHttpEndpoint());
    }

    [Fact]
    public void GetPublicEndpoint_prefers_the_hostname_to_the_onion_service()
    {
        Assert.Equal("https://node.example.com", TestConfiguration.Create(
            ("Hostname", "https://node.example.com"), ("OnionService", "http://example.onion")).GetPublicEndpoint());
        Assert.Equal("http://example.onion", TestConfiguration.Create(("OnionService", "http://example.onion")).GetPublicEndpoint());
        Assert.Null(TestConfiguration.Create().GetPublicEndpoint());
    }

    [Fact]
    public void Share_and_file_urls_are_built_from_the_public_endpoint()
    {
        var configuration = TestConfiguration.Create(("Hostname", "https://node.example.com"));

        Assert.Equal($"https://node.example.com/Share?Tag={Tag}", configuration.GetSharedDataUrl(Tag));
        Assert.Equal($"https://node.example.com/File?Tag={Tag}", configuration.GetFileUrl(Tag));
    }

    [Fact]
    public void Share_and_file_urls_are_null_without_a_public_endpoint()
    {
        var configuration = TestConfiguration.Create();

        Assert.Null(configuration.GetSharedDataUrl(Tag));
        Assert.Null(configuration.GetFileUrl(Tag));
    }

    [Fact]
    public void Validity_dates_lie_one_retention_period_ahead()
    {
        var configuration = TestConfiguration.Create(("SharedDataRetentionPeriod", "01.00:00:00"), ("FilesRetentionPeriod", "02.00:00:00"));
        var before = DateTimeOffset.UtcNow;

        var sharedData = configuration.GetSharedDataValidityDate();
        var file = configuration.GetFileValidityDate();

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(sharedData!.Value, before + TimeSpan.FromDays(1), after + TimeSpan.FromDays(1));
        Assert.InRange(file!.Value, before + TimeSpan.FromDays(2), after + TimeSpan.FromDays(2));
    }

    #endregion

    #region Paths of uploaded files

    [Fact]
    public void GetWebContentFilePath_places_a_tag_inside_the_upload_folder()
    {
        using var folder = new TempFolder();
        var configuration = TestConfiguration.Create(("WebContentDirectory", folder.Path));

        Assert.Equal(Path.Combine(folder.Path, Tag), configuration.GetWebContentFilePath(Tag));
    }

    [Fact]
    public void GetWebContentFilePath_uses_the_lower_case_form_of_the_tag()
    {
        using var folder = new TempFolder();
        var configuration = TestConfiguration.Create(("WebContentDirectory", folder.Path + Path.DirectorySeparatorChar));

        Assert.Equal(Path.Combine(folder.Path, Tag), configuration.GetWebContentFilePath(Tag.ToUpperInvariant()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("file.txt")]
    [InlineData("../private-key.pem")]
    [InlineData("/etc/passwd")]
    [InlineData("../3f2504e0-4f89-11d3-9a0c-0305e82c3301")]
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301/../x")]
    public void GetWebContentFilePath_refuses_a_tag_that_is_not_a_guid(string? tag)
    {
        using var folder = new TempFolder();
        var configuration = TestConfiguration.Create(("WebContentDirectory", folder.Path));

        Assert.Null(configuration.GetWebContentFilePath(tag));
    }

    #endregion
}
