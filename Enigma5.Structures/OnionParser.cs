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

using System.Runtime.InteropServices;
using Enigma5.Crypto;
using Enigma5.Crypto.Contracts;
using Enigma5.Structures.Contracts;

namespace Enigma5.Structures;

public class OnionParser : IDisposable
{
    private readonly IEnvelopeUnseal _unsealService;

    public byte[]? Next { get; private set; }

    public string? NextAddress { get; private set; }

    public byte[]? Content { get; private set; }

    private OnionParser(IEnvelopeUnseal unsealService)
    {
        _unsealService = unsealService;
    }

    ~OnionParser()
    {
        _unsealService.Dispose();
    }

    public void Dispose()
    {
        _unsealService.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Reset()
    {
        Next = null;
        NextAddress = null;
        Content = null;
    }

    public bool Parse(IOnion onion)
    {
        var data = _unsealService.UnsealOnion(onion.Content, out int outLen);

        if (data == IntPtr.Zero || outLen < 0)
        {
            return false;
        }

        var contentLen = outLen - Constants.DefaultAddressSize;

        Next = new byte[Constants.DefaultAddressSize];
        Content = new byte[contentLen];

        Marshal.Copy(data, Next, 0, Constants.DefaultAddressSize);
        Marshal.Copy(data + Constants.DefaultAddressSize, Content, 0, contentLen);
        NextAddress = HashProvider.ToHex(Next);

        return true;
    }

    public static class Factory
    {
        public static OnionParser Create(byte[] key, string passphrase)
        {
            return new OnionParser(Envelope.Factory.CreateUnseal(key, passphrase));
        }

        public static OnionParser CreateFromFile(string path, string passphrase)
        {
            return new OnionParser(Envelope.Factory.CreateUnsealFromFile(path, passphrase));
        }
    }
}
