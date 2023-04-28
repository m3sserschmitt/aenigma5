using Message.Contracts;
using Crypto;

namespace Message;

public class OnionBuilder
{
    private class Impl :
        ISetMessageContent,
        ISetMessageNextAddress,
        IEncryptMessage,
        IOnionBuilder
    {
        private IOnion Onion = new Onion();

        public IOnion Build()
        {
            return Onion;
        }

        private void Seal(Func<byte[], byte[]?> executor)
        {
            var ciphertext = executor(Onion.Content);

            if(ciphertext == null)
            {
                throw new Exception("Encryption failed.");
            }

            Onion.Content = ciphertext;
        }

        public IOnionBuilder Seal(string key)
        {
            using var seal = Envelope.Factory.CreateSeal(key, 2048);
            Seal(seal.Seal);

            return this;
        }

        public IOnionBuilder SealEx(string keyPath)
        {
            using var seal = Envelope.Factory.CreateSealFromFile(keyPath, 2048);
            Seal(seal.Seal);

            return this;
        }

        public IEncryptMessage SetNextAddress(byte[] address)
        {
            if(address.Length > 32)
            {
                throw new ArgumentException("Destination address length cannot exceed 32 bytes.");
            }

            Onion.Content = address.Concat(Onion.Content).ToArray();
            return this;
        }

        public ISetMessageNextAddress SetMessageContent(byte[] content)
        {
            Onion.Content = (byte[]) content.Clone();
            return this;
        }
    }

    public static ISetMessageContent Create()
    {
        return new Impl();
    }
}
