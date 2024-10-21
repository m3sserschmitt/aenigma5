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

using Enigma5.Structures.Contracts;
using Enigma5.Crypto;
using System.Runtime.InteropServices;

namespace Enigma5.Structures;

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
            if (address.Length != Constants.DefaultAddressSize)
            {
                throw new ArgumentException($"Destination address length should be exactly {Constants.DefaultAddressSize} bytes long.");
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

        public static bool MessageSizeExceeded(byte[] content)
        {
            return content.Length >= ushort.MaxValue
            || Envelope.GetEnvelopeSize(content.Length) > ushort.MaxValue;
        }
    }

    public static unsafe byte[]? CreateOnion(byte[] plaintext, string[] keys, string[] addresses)
    {
        if(keys.Length != addresses.Length)
        {
            throw new ArgumentException("Number of keys should be equal with the number of addresses.");
        }

        IntPtr data = Envelope.SealOnion(plaintext, keys, addresses, out int outLen);
        
        if(data == IntPtr.Zero)
        {
            return null;
        }

        var output = new byte[outLen];

        Marshal.Copy(data, output, 0, outLen);
        Marshal.Copy(new byte[outLen], 0, data, outLen);
        Marshal.FreeHGlobal(data);
        
        return output;
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
