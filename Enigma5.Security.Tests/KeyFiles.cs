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
using Microsoft.Extensions.Configuration;

namespace Enigma5.Security.Tests;

// A folder with the two key files of a node, and the settings that point to them.
internal sealed class KeyFiles : IDisposable
{
    private readonly TempFolder _folder = new();

    public string PrivateKeyPath => _folder.File("private-key.pem");

    public string PublicKeyPath => _folder.File("public-key.pem");

    // The passphrase is kept only for the life of the process, in a keyring of the test run.
    public IConfiguration Configuration => TestConfiguration.Create(
        ("PrivateKeyPath", PrivateKeyPath),
        ("PublicKeyPath", PublicKeyPath),
        ("PassphrasePersistence", "Ephemeral"),
        ("Hostname", "https://node.example.com"),
        ("OnionService", "http://example.onion"));

    public static KeyFiles Empty() => new();

    public static KeyFiles For(string privateKey)
    {
        var files = new KeyFiles();
        File.WriteAllText(files.PrivateKeyPath, privateKey);
        File.WriteAllText(files.PublicKeyPath, TestKeys.PublicKeyOf(privateKey));
        return files;
    }

    public void Dispose() => _folder.Dispose();
}
