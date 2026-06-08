/*
    Aenigma - Federated messaging system
    Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Enigma5.Security;

public static class KeysGenerator
{
    private const int KeySizeBits = 4096;

    private const string GenerateKeyCommand = "openssl";

    private const string GenerateKeyArguments = "genrsa -aes256 -out {0} -passout stdin {1}";

    private const string GenerateUnprotectedKeyArguments = "genrsa -out {0} {1}";

    private const string ExportPublicKeyCommand = GenerateKeyCommand;

    private const string ExportPublicKeyArguments = "rsa -in {0} -outform PEM -pubout -out {1} -passin stdin";

    private const string ExportPublicKeyUnprotectedArguments = "rsa -in {0} -outform PEM -pubout -out {1}";

    private static string GetGenerateKeyArguments(char[] passphrase)
    => passphrase.Length == 0 ? GenerateUnprotectedKeyArguments : GenerateKeyArguments;

    private static string GetExportPublicKeyArguments(char[] passphrase)
    => passphrase.Length == 0 ? ExportPublicKeyUnprotectedArguments : ExportPublicKeyArguments;

    public static Task<bool> Generate(string privatePemPath, char[] passphrase, int keySize = KeySizeBits, ILogger? logger = null)
    => LaunchKeyProcess(GenerateKeyCommand, string.Format(GetGenerateKeyArguments(passphrase), privatePemPath, keySize), passphrase, logger);

    public static Task<bool> ExportPublicKey(string privatePemPath, string publicPemPath, char[] passphrase, ILogger? logger = null)
    => LaunchKeyProcess(ExportPublicKeyCommand, string.Format(GetExportPublicKeyArguments(passphrase), privatePemPath, publicPemPath), passphrase, logger);

    private static async Task<bool> LaunchKeyProcess(string command, string arguments, char[] passphrase, ILogger? logger = null)
    {
        try
        {
            using var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            if (!process.Start())
            {
                return false;
            }
            await process.StandardInput.WriteAsync(passphrase);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
            await process.WaitForExitAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error encountered while running key generator process.");
            return false;
        }
    }
}
