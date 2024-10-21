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

using Enigma5.App.Common.Extensions;
using Enigma5.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public class AzurePassphraseReader(
    IConfiguration configuration,
    AzureClient azureClient,
    ILogger<AzurePassphraseReader> logger) : IPassphraseProvider
{
    private static readonly string PASSPHRASE_NAME_NOT_PROVIDED = "Passphrase name not provided";

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<AzurePassphraseReader> _logger = logger;

    private readonly AzureClient _azureClient = azureClient;

    public char[] ProvidePassphrase()
    {
        try
        {
            var passphrasePath = _configuration.GetPassphraseKeyPath() ?? throw new Exception(PASSPHRASE_NAME_NOT_PROVIDED);
            return _azureClient.GetSecret(passphrasePath).ToCharArray();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error encountered while trying to read passphrase from vault.");
            throw;
        }
    }
}
