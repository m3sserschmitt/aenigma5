/*
    Aenigma - Federal messaging system
    Copyright © 2024-2025 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public class AzureKeysReader(
    IConfiguration configuration,
    AzureClient azureClient,
    ILogger<AzureKeysReader> logger) : KeyReader(configuration)
{
    private readonly ILogger<AzureKeysReader> _logger = logger;

    private readonly AzureClient _azureClient = azureClient;

    public override async Task<string?> ReadPublicKeyAsync()
    {
        try
        {
            if(string.IsNullOrWhiteSpace(PublicKeyPath))
            {
                throw new Exception("Public key path is null or empty.");
            }
            return await _azureClient.GetSecretAsync(PublicKeyPath);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error occurred while reading public key.");
            return null;
        }
    }

    public override async Task<string?> ReadPrivateKeyAsync()
    {
        try
        {
            if(string.IsNullOrWhiteSpace(PrivateKeyPath))
            {
                throw new Exception("Private key path is null or empty.");
            }
            return await _azureClient.GetSecretAsync(PrivateKeyPath);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error occurred while reading private key.");
            return null;
        }
    }
}
