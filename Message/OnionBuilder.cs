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
            Onion.Content = EncodeSize((ushort)Onion.Content.Length).Concat(Onion.Content).ToArray();
            return Onion;
        }

        private void Seal(Func<byte[], byte[]?> executor)
        {
            var ciphertext = executor(Onion.Content);

            if (ciphertext == null)
            {
                throw new Exception("Message encryption failed.");
            }

            Onion.Content = ciphertext;
        }

        public IOnionBuilder Seal(string key)
        {
            using var seal = Envelope.Factory.CreateSeal(key);
            Seal(seal.Seal);

            return this;
        }

        public IOnionBuilder SealEx(string keyPath)
        {
            using var seal = Envelope.Factory.CreateSealFromFile(keyPath);
            Seal(seal.Seal);

            return this;
        }

        public IEncryptMessage SetNextAddress(byte[] address)
        {
            if (address.Length != AddressContext.Current.AddressSize)
            {
                throw new ArgumentException($"Destination address length should be exactly {AddressContext.Current.AddressSize} bytes long.");
            }

            Onion.Content = address.Concat(Onion.Content).ToArray();
            return this;
        }

        public ISetMessageNextAddress SetMessageContent(byte[] content)
        {
            if(content.Length > ushort.MaxValue)
            {
                throw new ArgumentException($"Message content should not exceed {ushort.MaxValue} bytes.");
            }

            Onion.Content = (byte[])content.Clone();
            return this;
        }

        private static byte[] EncodeSize(ushort size)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)(size / 256);
            buffer[1] = (byte)(size % 256);

            return buffer;
        }
    }

    public static ISetMessageContent Create()
    {
        return new Impl();
    }
}
