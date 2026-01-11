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

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Enigma5.App.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public class AzureClient(IConfiguration configuration, ILogger<AzureKeysReader> logger)
{
    private static readonly string KEYS_LOCATION_NOT_PROVIDED = "Azure Vault URL is null or empty.";

    private static readonly string COULD_NOT_CREATE_AZURE_CLIENT = "Could not create Azure Client.";

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<AzureKeysReader> _logger = logger;

    private string? Url
    {
        get
        {
            try
            {
                var azureVaultUrl = _configuration.GetAzureVaultUrl();
                if(string.IsNullOrWhiteSpace(azureVaultUrl))
                {
                    throw new Exception(KEYS_LOCATION_NOT_PROVIDED);
                }
                return azureVaultUrl;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error encountered while trying to retrieve keys location from config.");
                return null;
            }
        }
    }

    public async Task<string?> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateSecretClient() ?? throw new Exception(COULD_NOT_CREATE_AZURE_CLIENT);
            return (await client.GetSecretAsync(name, version, cancellationToken)).Value.Value; ;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error encountered while trying to read secret from vault.");
            return null;
        }
    }

    private SecretClient? CreateSecretClient() => !string.IsNullOrWhiteSpace(Url) ? new(new Uri(Url), new DefaultAzureCredential()) : null;
}
