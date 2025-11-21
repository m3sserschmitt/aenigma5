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

namespace Enigma5.Security;

public class FileKeyReader(IConfiguration configuration): KeyReader(configuration)
{
    private const string PRIVATE_KEY_FILE_NOT_FOUND_ERROR_MESSAGE = "Private Key file not found.";

    private const string PUBLIC_KEY_NOT_FOUND_ERROR_MESSAGE = "Public Key file not found.";

    public override string ReadPublicKey()
    {
        if(!File.Exists(PublicKeyPath))
        {
            throw new Exception(PUBLIC_KEY_NOT_FOUND_ERROR_MESSAGE);
        }

        return File.ReadAllText(PublicKeyPath);
    }

    public override string ReadPrivateKey()
    {
        if(!File.Exists(PrivateKeyPath))
        {
            throw new Exception(PRIVATE_KEY_FILE_NOT_FOUND_ERROR_MESSAGE);
        }

        return File.ReadAllText(PrivateKeyPath);
    }
}
