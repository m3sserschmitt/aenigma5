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

using Enigma5.App.Common.Extensions;
using Enigma5.Crypto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace NetworkBridge;

public class ConfigurationLoader
{
    private string? _previousHash;

    public readonly string ConfigPath;

    public event Action? OnConfigurationReloaded;

    public IConfigurationRoot Configuration { get; private set; }

    public ConfigurationLoader(string file)
    {
        ConfigPath = Path.Combine(Directory.GetCurrentDirectory(), file);
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(file, false, true)
            .Build();
        
        _previousHash = ComputeConfigHash();
        PrintSettings();
        ChangeToken.OnChange(() => Configuration.GetReloadToken(), OnChange);
    }

    private void OnChange()
    {
        var currentHash = ComputeConfigHash();
        if(currentHash == _previousHash)
        {
            return;
        }
        _previousHash = currentHash;
        Console.WriteLine("[+] Configuration reloaded.");
        PrintSettings();
        OnConfigurationReloaded?.Invoke();
    }

    private void PrintSettings()
    {
        Console.WriteLine($"Peers: {string.Join(", ", Configuration.GetPeers() ?? [])}");
        Console.WriteLine($"Kestrel Http Url: {Configuration.GetLocalListenAddress()}");
        Console.WriteLine($"RetryConnection: {Configuration.GetRetryConnection()}");
        Console.WriteLine($"ConnectionRetriesCount: {Configuration.GetConnectionRetriesCount()}");
        Console.WriteLine($"DelayBetweenConnectionRetries: {Configuration.GetDelayBetweenConnectionRetries()}");
        Console.WriteLine($"PrivateKeyPath: {Configuration.GetPrivateKeyPath()}");
        Console.WriteLine($"PublicKeyPath: {Configuration.GetPublicKeyPath()}");
    }

    private string? ComputeConfigHash()
    {
        try
        {
            using var fileStream = File.OpenRead(ConfigPath);
            using var streamReader = new StreamReader(fileStream);
            return HashProvider.Sha256Hex(streamReader.ReadToEnd());
        }
        catch (Exception)
        {
           return null;
        }
    }
}
