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

using System.ComponentModel.DataAnnotations;

namespace Enigma5.App.Data;

public class SharedData(string data, string publicKey, int maxAccessCount = 1)
{
    [Key]
    public string Tag { get; set; } = Guid.NewGuid().ToString();

    public string Data { get; set; } = data;

    public string PublicKey { get; set; } = publicKey;

    public int AccessCount { get; set; }

    public int MaxAccessCount { get; set; } = maxAccessCount;

    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
}
