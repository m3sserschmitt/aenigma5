using Enigma5.Message.Contracts;
using Enigma5.Crypto;
using Enigma5.Core;

namespace Enigma5.Message;

public class OnionBuilder
{
    private class Impl :
        ISetMessageContent,
        ISetMessageNextAddress,
        IEncryptMessage,
        IOnionBuilder
    {
        private IOnion Onion;

        public Impl()
        {
            Onion = new Onion();
        }

        public Impl(IOnion onion)
        {
            Onion = onion;
        }

        public IOnion Build()
        {
            return Onion;
        }

        public ISetMessageNextAddress AddPeel()
        {
            SetMessageContent(Onion.Content);

            return this;
        }

        private void Seal(Func<byte[], byte[]?> executor)
        {
            var ciphertext = executor(Onion.Content);

            if (ciphertext == null)
            {
                throw new Exception("Message encryption failed.");
            }

            Onion.Content = ciphertext;
            Onion.Content = EncodeSize((ushort)Onion.Content.Length).Concat(Onion.Content).ToArray();
        }

        public IOnionBuilder Seal(string key)
        {
            using (var seal = Envelope.Factory.CreateSeal(key))
            {
                Seal(seal.Seal);
            }

            return this;
        }

        public IOnionBuilder SealEx(string keyPath)
        {
            using (var seal = Envelope.Factory.CreateSealFromFile(keyPath))
            {
                Seal(seal.Seal);
            }

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
            if (MessageSizeExceeded(content))
            {
                throw new ArgumentException($"Maximum size for content exceeded.");
            }

            if (ReferenceEquals(Onion.Content, content))
            {
                return this;
            }

            Onion.Content = (byte[])content.Clone();
            return this;
        }

        public static byte[] EncodeSize(ushort size)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)(size / 256);
            buffer[1] = (byte)(size % 256);

            return buffer;
        }

        public bool MessageSizeExceeded(byte[] content)
        {
            return content.Length >= ushort.MaxValue
                ? true
                : SealProvider.GetEnvelopeSize(2048, (ushort)content.Length) > ushort.MaxValue;
        }
    }

    public static ISetMessageContent Create()
    {
        return new Impl();
    }

    public static IOnionBuilder Create(IOnion onion)
    {
        return new Impl(onion);
    }
}
