/*
    Aenigma - Federal messaging system
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

using Enigma5.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Enigma5.App.Common.Extensions;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public class AzureKeysReader(
    IConfiguration configuration,
    AzureClient azureClient,
    ILogger<AzureKeysReader> logger) : IKeysReader
{
    private static readonly string PUBLIC_KEY_SECRET_NAME_NOT_PROVIDED = "Public key secret name not provided.";

    private static readonly string PRIVATE_KEY_SECRET_NAME_NOT_PROVIDED = "Private key secret name not provided.";

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<AzureKeysReader> _logger = logger;

    private readonly AzureClient _azureClient = azureClient;

    public byte[] PrivateKey => ReadPrivateKey();

    public string PublicKey => ReadPublicKey();

    private string ReadPublicKey()
    {
        try
        {
            var publicKeyPath = _configuration.GetPublicKeyPath() ?? throw new Exception(PUBLIC_KEY_SECRET_NAME_NOT_PROVIDED);
            return _azureClient.GetSecret(publicKeyPath);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error occurred while reading public key.");
            throw;
        }
    }

    private byte[] ReadPrivateKey()
    {
        try
        {
            var publicKeyPath = _configuration.GetPrivateKeyPath() ?? throw new Exception(PRIVATE_KEY_SECRET_NAME_NOT_PROVIDED);
            return Encoding.UTF8.GetBytes(_azureClient.GetSecret(publicKeyPath));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error occurred while reading private key.");
            throw;
        }
    }
}
