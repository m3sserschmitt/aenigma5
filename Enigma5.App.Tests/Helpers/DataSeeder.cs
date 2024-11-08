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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Enigma5.App.Common.Extensions;
using Enigma5.Crypto;
using Enigma5.Crypto.DataProviders;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Tests.Helpers;

[ExcludeFromCodeCoverage]
public class DataSeeder(Enigma5.App.Data.EnigmaDbContext dbContext)
{
    private readonly Enigma5.App.Data.EnigmaDbContext _dbContext = dbContext;

    public static class ModelsFactory
    {
        public static Models.SharedDataCreate CreateSharedDataCreate()
        {
            var data = Encoding.UTF8.GetBytes("data to be shared");
            using var signer = SealProvider.Factory.CreateSigner(PKey.PrivateKey1, PKey.Passphrase);
            var signedData = signer.Sign(data);
            var encodedData = Convert.ToBase64String(signedData!);

            return new() {
                PublicKey = PKey.PublicKey1,
                SignedData = encodedData,
                AccessCount = 3
            };
        }

        public static Models.AuthenticationRequest CreateAuthenticationRequest(string nonce)
        {
            using var signature = SealProvider.Factory.CreateSigner(PKey.PrivateKey1, PKey.Passphrase);
            var decodedNonce = Convert.FromBase64String(nonce);
            var data = signature.Sign(decodedNonce);

            return new() {
                PublicKey = PKey.PublicKey1,
                Signature = Convert.ToBase64String(data!)
            };
        }

        public static Models.SignatureRequest CreateSignatureRequest()
        {
            var tokenData = new byte[64];
            new Random().NextBytes(tokenData);
            var token = Convert.ToBase64String(tokenData);

            return new() {
                Nonce = token
            };
        }

        public static string? CreateOnion(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            return CreateOnion(dataBytes);
        }

        public static string? CreateOnion(byte[] data)
        => SealProvider.SealOnion(data, [ PKey.PublicKey2, PKey.PublicKey3 ], [ PKey.Address1, PKey.Address2 ]);


        public static Models.RoutingRequest CreateRoutingRequest()
        => new() {
                Payload = CreateOnion("pending-message")
        };
    }

    public static class DataFactory
    {
        private static readonly Enigma5.App.Data.SharedData _sharedData = new ("shared-data", 2)
        {
            Tag = "acde070d-8c4c-4f0d-9d8a-162843c10333"
        };

        private static readonly Enigma5.App.Data.SharedData _oldSharedData = new ("old-shared-data", 2)
        {
            Tag = "acdeaddd-8c4c-4f0d-9d8a-162843c10355",
            DateCreated = DateTimeOffset.Now - TimeSpan.FromDays(2)
        };

        private static readonly Enigma5.App.Data.PendingMessage _pendingMesage = new (PKey.Address2, "pending-message", false)
        {
            Id = 1
        };

        private static readonly Enigma5.App.Data.PendingMessage _oldPendingMesage = new (PKey.Address2, "old-pending-message", false)
        {
            Id = 2,
            DateReceived = DateTimeOffset.Now - TimeSpan.FromDays(5)
        };


        private static readonly Enigma5.App.Data.PendingMessage _deliveredPendingMesage = new (PKey.Address2, "delivered-pending-message", true)
        {
            Id = 3
        };

        private static readonly Enigma5.App.Data.AuthorizedService _authorizedService = new(1, PKey.Address1);

        public static Enigma5.App.Data.SharedData SharedData => _sharedData.CopyBySerialization();

        public static Enigma5.App.Data.SharedData OldSharedData => _oldSharedData.CopyBySerialization();

        public static Enigma5.App.Data.PendingMessage PendingMesage => _pendingMesage.CopyBySerialization();

        public static Enigma5.App.Data.PendingMessage OldPendingMesage => _oldPendingMesage.CopyBySerialization();

        public static Enigma5.App.Data.PendingMessage DeliveredPendingMesage => _deliveredPendingMesage.CopyBySerialization();
        
        public static Enigma5.App.Data.AuthorizedService AuthorizedService => _authorizedService.CopyBySerialization();
    }

    public async Task Seed()
    {
        _dbContext.SharedData.RemoveRange(_dbContext.SharedData);
        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        _dbContext.AuthorizedServices.RemoveRange(_dbContext.AuthorizedServices);

        await _dbContext.SaveChangesAsync();

        _dbContext.SharedData.Add(DataFactory.SharedData);
        _dbContext.SharedData.Add(DataFactory.OldSharedData);
        _dbContext.Messages.Add(DataFactory.PendingMesage);
        _dbContext.Messages.Add(DataFactory.OldPendingMesage);
        _dbContext.Messages.Add(DataFactory.DeliveredPendingMesage);
        _dbContext.AuthorizedServices.Add(DataFactory.AuthorizedService);
        
        await _dbContext.SaveChangesAsync();
    }

    public Task<Enigma5.App.Data.SharedData?> SharedData => _dbContext.SharedData.FirstOrDefaultAsync(item => item.Tag == DataFactory.SharedData.Tag);

    public Task<Enigma5.App.Data.PendingMessage?> PendingMessage => _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataFactory.PendingMesage.Id);

    public Task<Enigma5.App.Data.PendingMessage?> OldPendingMessage => _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataFactory.OldPendingMesage.Id);

    public Task<Enigma5.App.Data.PendingMessage?> DeliveredPendingMessage => _dbContext.Messages.FirstOrDefaultAsync(item => item.Id == DataFactory.DeliveredPendingMesage.Id);

    public Task<Enigma5.App.Data.AuthorizedService?> AuthorizedService => _dbContext.AuthorizedServices.FirstOrDefaultAsync(item => item.Id == DataFactory.AuthorizedService.Id);
}
