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

using Enigma5.Crypto;
using Enigma5.Crypto.Contracts;
using Enigma5.Structures.Contracts;

namespace Enigma5.Structures;

public class OnionParser(IEnvelopeUnsealer envelopeUnseal)
{
    private readonly IEnvelopeUnsealer _unsealService = envelopeUnseal;

    public string? NextAddress { get; private set; }

    public byte[]? Content { get; private set; }

    public bool Parse(IOnion onion)
    {
        try
        {
            var data = _unsealService.UnsealOnion(onion.Content, out int outLen);

            if (data == IntPtr.Zero || outLen < Constants.AddressSize)
            {
                return false;
            }

            var nextAddress = KeyUtil.CopyKeyFromNativeBuffer(data, Constants.AddressSize);
            Content = KeyUtil.CopyKeyFromNativeBuffer(data + Constants.AddressSize, outLen - Constants.AddressSize);
            NextAddress = HashProvider.ToHex(nextAddress);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
