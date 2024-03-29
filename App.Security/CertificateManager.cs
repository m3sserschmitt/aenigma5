using Enigma5.App.Common.Utils;
using Enigma5.App.Security.Contracts;
using Enigma5.Crypto;

namespace Enigma5.App.Security;

public class CertificateManager : ICertificateManager
{
    private readonly KeysProvider _keysProvider;

    private readonly SingleThreadExecutor<byte[]> _kernelQueryExecutor = new();

    private readonly object _locker = new();

    private const string PRIVATE_KEY_READING_ERROR_MESSAGE = "Could not read Private Key from Kernel.";

    private const string PRIVATE_KEY_CACHING_ERROR = "Could not cache Private Key into Kernel.";

    private const string KERNEL_KEY_NAME = "ENIGMA_PRIVATE_KEY";

    private const string KERNEL_KEY_DESCRIPTION = "enigma5key: Key used for cryptographic operations.";

    private const string KERNEL_KEY_NOT_FOUND_ERROR_MESSAGE = "Key not found into Kernel.";

    private const KernelKeyring THREAD_KEYRING = KernelKeyring.ThreadKeyring;

    public string PublicKey { get => ThreadSafeExecution.Execute(() => _keysProvider.PublicKey, string.Empty, _locker); }

    public byte[] PrivateKey { get => ReadKeyFromKernel(); }

    public string Address { get => ThreadSafeExecution.Execute(() => CertificateHelper.GetHexAddressFromPublicKey(PublicKey), string.Empty, _locker); }

    public CertificateManager(KeysProvider keysProvider)
    {
        _keysProvider = keysProvider;
        _kernelQueryExecutor.StartLooper();
        ReadPrivateKeyFromFile();
    }

    private Action CacheKeyIntoKernelAction => () =>
    {
        var privateKeyBytes = _keysProvider.PrivateKey;

        if (KernelKey.Create(KERNEL_KEY_NAME, privateKeyBytes, KERNEL_KEY_DESCRIPTION, THREAD_KEYRING) < 0)
        {
            Array.Clear(privateKeyBytes);
            throw new Exception(PRIVATE_KEY_CACHING_ERROR);
        }

        Array.Clear(privateKeyBytes);
    };

    private static Func<byte[]> ReadKeyFromKernelAction => () =>
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

    private byte[] ReadKeyFromKernel()
    {
        var result = _kernelQueryExecutor.Execute(ReadKeyFromKernelAction);

        if (result.Exception is not null)
        {
            throw result.Exception;
        }

        return result.Value!;
    }
}
