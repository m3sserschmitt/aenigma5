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

using System.Text;

namespace Enigma5.Crypto.DataProviders;

public class TestSignature
{
    public bool IsValid { get; set; }

    private byte[] Signature { get; set; }

    private TestSignature(bool isValid)
    {
        IsValid = isValid;

        var data = new byte[32];
        new Random().NextBytes(data);

        using var signature = Envelope.Factory.CreateSignature(Encoding.UTF8.GetBytes(PKey.PrivateKey1), PKey.Passphrase);
        Signature = signature.Sign(data) ?? new byte[1];

        if (!isValid && Signature.Length > 1)
        {
            Signature[data.Length + 10] ^= 255;
        }
    }

    public static implicit operator byte[](TestSignature signature)
    {
        return signature.Signature;
    }

    public static TestSignature CreateValidSignature()
    {
        return new TestSignature(true);
    }

    public static TestSignature CreateInvalidSignature()
    {
        return new TestSignature(false);        
    }
}
