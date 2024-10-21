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

namespace Enigma5.App.Models.Extensions;

public static class SignatureExtensions
{
    public static byte[]? GetDataFromSignature(this byte[]? signature)
    {
        if (signature == null)
        {
            return null;
        }

        var digestLength = Crypto.Constants.DefaultPKeySize / 8;

        if (signature.Length < digestLength + 1)
        {
            return null;
        }

        return signature[..^digestLength];
    }

    public static string? GetStringDataFromSignature(this byte[]? signature)
    {
        var data = signature.GetDataFromSignature();

        if (data == null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(data);
    }
}
