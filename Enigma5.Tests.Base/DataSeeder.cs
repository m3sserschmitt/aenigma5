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

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Enigma5.App.Common.Extensions;
using Enigma5.Crypto;
using Enigma5.Crypto.DataProviders;

namespace Enigma5.Tests.Base;

[ExcludeFromCodeCoverage]
public class DataSeeder(App.Data.EnigmaDbContext dbContext)
{
    private readonly App.Data.EnigmaDbContext _dbContext = dbContext;

    public static class ModelsFactory
    {
        private static readonly App.Models.VertexBroadcastRequest _vertexBroadcastRequest = new(PKey.PublicKey1, "eyJIb3N0bmFtZSI6ImFkamFjZW50LWhvc3RuYW1lIiwiQWRkcmVzcyI6ImNiZmYyZTEyZmIxZjc1MmNiMTcxODVmMDgwZjJiNDAzMDExNjVhMTA1MTUzMWNjMDYxNGU0OTVlZTI2MjBlZjkiLCJOZWlnaGJvcnMiOlsiYmEzMWQyM2UyODIwMDNjNjc4ZGNmZWM0OGMxN2QwNDQwNjFmMzQwZDNlNmU3ZjdhMTEwOWRkMWRiMzJkMWQ5NSJdfX3Pv7iVh2pHYp/wK8Qnre/apsDbTA6NZwBjSpPzBI8tgeK8VvLi5XvR9fRJV4XO3C51YnDVSIXRzDJ6vgDE3jMmLMF38ZzA4UJ5iNSlJ9r0kcLKUYx3Aq16EEEFKPaHBqCv+Cd4csIzviOIPBqBeFi7WiFyq+/hoyV9DtBfcyYgEh79Wo3On4zXh5lGoGWwuhORDTTPKRgu54TZs1DXubpdj+4fzDYhCrEiYgl3Cw752g5fgXwb//kC/awJYop/BHFdx6EWd5wP0qlYUrig6upj1Kk3WueXbMaQRmIlWFzwHH1/IkElpbc1lcrLy2b6vK0CssgF1uP4qSrDliScuMM=");

        public static App.Models.VertexBroadcastRequest VertexBroadcastRequest => _vertexBroadcastRequest.CopyBySerialization();

        public static App.Models.SharedDataCreate CreateSharedDataCreate()
        {
            var data = Encoding.UTF8.GetBytes("data to be shared");
            using var signer = SealProvider.Factory.CreateSigner(PKey.PrivateKey1, PKey.Passphrase);
            var signedData = signer.Sign(data);
            var encodedData = Convert.ToBase64String(signedData!);

            return new(PKey.PublicKey1, encodedData, 3);
        }

        public static App.Models.AuthenticationRequest CreateAuthenticationRequest(string nonce)
        {
            using var signature = SealProvider.Factory.CreateSigner(PKey.PrivateKey1, PKey.Passphrase);
            var decodedNonce = Convert.FromBase64String(nonce);
            var data = signature.Sign(decodedNonce);

            return new(PKey.PublicKey1, Convert.ToBase64String(data!));
        }

        public static App.Models.SignatureRequest CreateSignatureRequest()
        {
            var tokenData = new byte[64];
            new Random().NextBytes(tokenData);
            var token = Convert.ToBase64String(tokenData);

            return new()
            {
                Nonce = token
            };
        }

        public static string? CreateOnion(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            return CreateOnion(dataBytes);
        }

        public static string? CreateOnion(byte[] data)
        => SealProvider.SealOnion(data, [PKey.PublicKey2, PKey.PublicKey3], [PKey.Address1, PKey.Address2]);


        public static App.Models.RoutingRequest CreateRoutingRequest()
        => new([CreateOnion("pending-message")!]);
    }

    public static class DataFactory
    {
        private static readonly App.Data.SharedData _sharedData = new()
        {
            Tag = "acde070d-8c4c-4f0d-9d8a-162843c10333",
            Data = "shared-data",
            PublicKey = PKey.PublicKey1,
            MaxAccessCount = 2
        };

        private static readonly App.Data.SharedData _oldSharedData = new()
        {
            Tag = "acdeaddd-8c4c-4f0d-9d8a-162843c10355",
            DateCreated = DateTimeOffset.Now - TimeSpan.FromDays(2),
            Data = "old-shared-data",
            PublicKey = PKey.PublicKey1,
            MaxAccessCount = 2
        };

        private static readonly App.Data.PendingMessage _pendingMessage = new()
        {
            Id = 1,
            Destination = PKey.Address2,
            Content = "pending-message",
            Sent = false
        };

        private static readonly App.Data.PendingMessage _oldPendingMessage = new()
        {
            Id = 2,
            DateReceived = DateTimeOffset.Now - TimeSpan.FromDays(5),
            Destination = PKey.Address2,
            Content = "old-pending-message",
            Sent = false
        };


        private static readonly App.Data.PendingMessage _deliveredPendingMessage = new()
        {
            Id = 3,
            Destination = PKey.Address2,
            Content = "delivered-pending-message",
            Sent = true,
            DateSent = DateTimeOffset.Now - TimeSpan.FromDays(3)
        };

        private static readonly App.Data.AuthorizedService _authorizedService = new()
        {
            Id = 1,
            Address = PKey.Address1
        };

        public static App.Data.SharedData SharedData => _sharedData.CopyBySerialization();

        public static App.Data.SharedData OldSharedData => _oldSharedData.CopyBySerialization();

        public static App.Data.PendingMessage PendingMessage => _pendingMessage.CopyBySerialization();

        public static App.Data.PendingMessage OldPendingMessage => _oldPendingMessage.CopyBySerialization();

        public static App.Data.PendingMessage DeliveredPendingMessage => _deliveredPendingMessage.CopyBySerialization();

        public static App.Data.AuthorizedService AuthorizedService => _authorizedService.CopyBySerialization();
    }

    public async Task Seed()
    {
        _dbContext.SharedData.RemoveRange(_dbContext.SharedData);
        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        _dbContext.AuthorizedServices.RemoveRange(_dbContext.AuthorizedServices);

        await _dbContext.SaveChangesAsync();

        _dbContext.SharedData.Add(DataFactory.SharedData);
        _dbContext.SharedData.Add(DataFactory.OldSharedData);
        _dbContext.Messages.Add(DataFactory.PendingMessage);
        _dbContext.Messages.Add(DataFactory.OldPendingMessage);
        _dbContext.Messages.Add(DataFactory.DeliveredPendingMessage);
        _dbContext.AuthorizedServices.Add(DataFactory.AuthorizedService);

        await _dbContext.SaveChangesAsync();
    }
}
