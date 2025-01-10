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

using Enigma5.App.Common.Utils;
using Enigma5.Crypto;
using Enigma5.Security.Contracts;

namespace Enigma5.Security;

public class CertificateManager : ICertificateManager
{
    private readonly IKeysReader _keysProvider;

    private readonly SingleThreadExecutor<string> _kernelQueryExecutor = new();

    private readonly object _locker = new();

    private const string PRIVATE_KEY_READING_ERROR_MESSAGE = "Could not read Private Key from Kernel.";

    private const string PRIVATE_KEY_CACHING_ERROR = "Could not cache Private Key into Kernel.";

    private const string KERNEL_KEY_NAME = "ENIGMA_PRIVATE_KEY";

    private const string KERNEL_KEY_DESCRIPTION = "enigma5key: Key used for cryptographic operations.";

    private const string KERNEL_KEY_NOT_FOUND_ERROR_MESSAGE = "Key not found into Kernel.";

    private const KernelKeyring THREAD_KEYRING = KernelKeyring.ThreadKeyring;

    public string PublicKey { get => ThreadSafeExecution.Execute(() => _keysProvider.PublicKey, string.Empty, _locker); }

    public string PrivateKey { get => ReadKeyFromKernel(); }

    public string Address { get => ThreadSafeExecution.Execute(() => CertificateHelper.GetHexAddressFromPublicKey(PublicKey), string.Empty, _locker); }

    public CertificateManager(IKeysReader keysProvider)
    {
        _keysProvider = keysProvider;
        _kernelQueryExecutor.StartLooper();
        ReadPrivateKeyFromFile();
    }

    private Action CacheKeyIntoKernelAction => () =>
    {
        if (KernelKey.Create(KERNEL_KEY_NAME, _keysProvider.PrivateKey, KERNEL_KEY_DESCRIPTION, THREAD_KEYRING) < 0)
        {
            throw new Exception(PRIVATE_KEY_CACHING_ERROR);
        }
    };

    private static Func<string> ReadKeyFromKernelAction => () =>
    {
        var privateKeyId = KernelKey.SearchKey(KERNEL_KEY_NAME, KERNEL_KEY_DESCRIPTION, THREAD_KEYRING);

        if (privateKeyId < 0)
        {
            throw new Exception(KERNEL_KEY_NOT_FOUND_ERROR_MESSAGE);
        }

        return KernelKey.ReadKey(privateKeyId) ?? throw new Exception(PRIVATE_KEY_READING_ERROR_MESSAGE);
    };

    private void ReadPrivateKeyFromFile()
    {
        var exception = _kernelQueryExecutor.Execute(CacheKeyIntoKernelAction);

        if (exception is not null)
        {
            throw exception;
        }
    }

    private string ReadKeyFromKernel()
    {
        var result = _kernelQueryExecutor.Execute(ReadKeyFromKernelAction);

        if (result.Exception is not null)
        {
            throw result.Exception;
        }

        return result.Value!;
    }
}
