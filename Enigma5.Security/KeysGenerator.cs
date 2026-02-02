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

using System.Diagnostics;

namespace Enigma5.Security;

public static class KeysGenerator
{
    private const int KeySizeBits = 4096;

    private const string GenerateKeyCommand = "openssl";

    private const string GenerateKeyArguments = "genrsa -aes256 -out {0} -passout stdin {1}";

    private const string ExportPublicKeyCommand = GenerateKeyCommand;

    private const string ExportPublicKeyArguments = "rsa -in {0} -outform PEM -pubout -out {1} -passin stdin";

    public static Task<bool> Generate(string privatePemPath, char[] passphrase, int keySize = KeySizeBits)
    => LaunchProcess(GenerateKeyCommand, string.Format(GenerateKeyArguments, privatePemPath, keySize), passphrase);

    public static Task<bool> ExportPublicKey(string privatePemPath, string publicPemPath, char[] passphrase)
    => LaunchProcess(ExportPublicKeyCommand, string.Format(ExportPublicKeyArguments, privatePemPath, publicPemPath), passphrase);

    private static async Task<bool> LaunchProcess(string command, string arguments, char[] passphrase)
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
        catch (Exception)
        {
            return false;
        }
    }
}
