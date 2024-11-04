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
using Enigma5.Crypto;
using Enigma5.Crypto.DataProviders;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Helpers;

public class DataSeeder(Enigma5.App.Data.EnigmaDbContext dbContext)
{
    private readonly Enigma5.App.Data.EnigmaDbContext _dbContext = dbContext;

    public static Enigma5.App.Data.SharedData CreateSharedData()
    {
        var data = Encoding.UTF8.GetBytes("data to be shared");
        using var signer = SealProvider.Factory.CreateSigner(PKey.PrivateKey1, PKey.Passphrase);
        var signedData = signer.Sign(data);
        var encodedData = Convert.ToBase64String(signedData!);

        return new (encodedData, 2);
    }

    public static Models.SharedDataCreate CreateSharedDataCreate()
    {
        var entity = CreateSharedData();

        return new() {
            PublicKey = PKey.PublicKey1,
            SignedData = entity.Data,
            AccessCount = 3
        };
    }

    public void Seed()
    {
        _dbContext.SharedData.Add(CreateSharedData());
        _dbContext.SaveChanges();
    }

    public Task<Enigma5.App.Data.SharedData?> SharedData => _dbContext.SharedData.FirstOrDefaultAsync();
}
