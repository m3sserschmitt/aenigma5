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

using Enigma5.Security.Contracts;

namespace Enigma5.Structures;

public class OnionParser(ICertificateManager certificateManager)
{
    private readonly ICertificateManager _certificateManager = certificateManager;

    public string? NextAddress { get; private set; }

    public byte[]? Content { get; private set; }

    public async Task<bool> ParseAsync(string onion)
    {
        try
        {
            string? next = null;
            byte[]? content = null;
            using var unsealer = await _certificateManager.CreateUnsealerAsync();
            if(unsealer.UnsealOnion(onion, ref next, ref content))
            {
                NextAddress = next;
                Content = content;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
