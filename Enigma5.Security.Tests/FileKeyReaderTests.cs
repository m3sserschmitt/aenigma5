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

using Enigma5.Tests.Base;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security.Tests;

public class FileKeyReaderTests
{
    private readonly CapturingLogger<FileKeyReader> _logger = new();

    [Fact]
    public async Task The_keys_are_read_from_the_files_named_by_the_settings()
    {
        using var files = KeyFiles.For(TestKeys.PrivateKey3);
        var reader = new FileKeyReader(files.Configuration, _logger);

        Assert.Equal(files.PrivateKeyPath, reader.PrivateKeyPath);
        Assert.Equal(files.PublicKeyPath, reader.PublicKeyPath);
        Assert.Equal(TestKeys.PrivateKey3, await reader.ReadPrivateKeyAsync());
        Assert.Equal(TestKeys.PublicKey3, await reader.ReadPublicKeyAsync());
        Assert.Equal(TestKeys.PrivateKey3, reader.ReadPrivateKey());
        Assert.Equal(TestKeys.PublicKey3, reader.ReadPublicKey());
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task A_missing_key_file_gives_null_and_a_critical_log_entry()
    {
        using var files = KeyFiles.Empty();
        var reader = new FileKeyReader(files.Configuration, _logger);

        Assert.Null(await reader.ReadPrivateKeyAsync());
        Assert.Null(await reader.ReadPublicKeyAsync());

        Assert.Equal(2, _logger.Entries.Count(entry => entry.Level == LogLevel.Critical));
    }

    [Fact]
    public void Without_settings_the_default_paths_are_used()
    {
        var reader = new FileKeyReader(TestConfiguration.Create(), _logger);

        Assert.Equal(App.Common.Constants.DefaultPrivateKeyPath, reader.PrivateKeyPath);
        Assert.Equal(App.Common.Constants.DefaultPublicKeyPath, reader.PublicKeyPath);
    }
}
