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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;

namespace Enigma5.Security;

public static class KeysGenerator
{
    public static (string PublicKey, string PrivateKey) GenerateKeys(int keySizeBits)
    {
        var keyPair = GenerateRsaKeyPair(keySizeBits);

        string publicKeyPem = ExportToPem(keyPair.Public);
        string privateKeyPem = ExportToPem(keyPair.Private);

        return (publicKeyPem, privateKeyPem);
    }

    private static AsymmetricCipherKeyPair GenerateRsaKeyPair(int keySizeBits)
    {
        var generator = new RsaKeyPairGenerator();
        var keyGenParam = new KeyGenerationParameters(
            new SecureRandom(),
            keySizeBits
        );
        generator.Init(keyGenParam);
        return generator.GenerateKeyPair();
    }

    private static string ExportToPem(AsymmetricKeyParameter key)
    {
        using var stringWriter = new StringWriter();
        var pemWriter = new PemWriter(stringWriter);
        pemWriter.WriteObject(key);
        return stringWriter.ToString();
    }
}
