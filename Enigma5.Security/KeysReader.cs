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

using System.Text;
using Enigma5.App.Common.Extensions;
using Enigma5.Security.Contracts;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Enigma5.Security;

public class KeysReader(IPassphraseProvider passphraseProvider, IConfiguration configuration): IKeysReader
{
    private const string PUBLIC_KEY_NOT_CONFIGURED_ERROR_MESSAGE = "Public Key file not configured.";

    private const string PRIVATE_KEY_NOT_CONFIGURED_ERROR_MESSAGE = "Private Key file not configured.";

    private const string PRIVATE_KEY_FILE_NOT_FOUND_ERROR_MESSAGE = "Private Key file not found.";

    private const string PUBLIC_KEY_NOT_FOUND_ERROR_MESSAGE = "Public Key file not found.";

    private const string INVALID_PRIVATE_KEY_PEM_OBJECT = "Private key PEM is invalid.";
    
    public string PublicKeyPath => configuration.GetPublicKeyPath() ?? throw new Exception(PUBLIC_KEY_NOT_CONFIGURED_ERROR_MESSAGE);

    public string PrivateKeyPath => configuration.GetPrivateKeyPath() ?? throw new Exception(PRIVATE_KEY_NOT_CONFIGURED_ERROR_MESSAGE);

    public bool PublicKeyFileExists => File.Exists(configuration.GetPublicKeyPath());

    public bool PrivateKeyFileExists => File.Exists(configuration.GetPrivateKeyPath());

    public string PrivateKey => ExportToPem(ReadPrivateKeyFile());

    public string PublicKey => ReadPublicKeyFile();

    private string ReadPublicKeyFile()
    {
        if(!PublicKeyFileExists)
        {
            throw new Exception(PUBLIC_KEY_NOT_FOUND_ERROR_MESSAGE);
        }

        return File.ReadAllText(PublicKeyPath);
    }

    private AsymmetricKeyParameter ReadPrivateKeyFile()
    {
        if(!PrivateKeyFileExists)
        {
            throw new Exception(PRIVATE_KEY_FILE_NOT_FOUND_ERROR_MESSAGE);
        }

        using var reader = File.OpenText(PrivateKeyPath);
        using var pemReader = new PemReader(reader);

        var pemObject = pemReader.ReadPemObject()
        ?? throw new Exception(INVALID_PRIVATE_KEY_PEM_OBJECT);

        reader.Close();
        pemReader.Reader.Close();
        var passphrase = passphraseProvider.ProvidePassphrase();
        var keyParameter = PrivateKeyFactory.DecryptKey(passphrase, pemObject.Content);
        Array.Clear(passphrase);

        return keyParameter;
    }

    private static string ExportToPem(AsymmetricKeyParameter key)
    {
        var stringBuilder = new StringBuilder();
        using var stringWriter = new StringWriter(stringBuilder);
        using var pemWriter = new PemWriter(stringWriter);

        pemWriter.WriteObject(key);
        
        pemWriter.Writer.Flush();
        pemWriter.Writer.Close();
        stringWriter.Flush();
        stringWriter.Close();

        return stringBuilder.ToString();
    }
}
