/*
    Aenigma - Federated messaging system
    Copyright © 2023-2026 Romulus-Emanuel Ruja <romulus.ruja@aenigma.ro>

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

using System.Linq.Expressions;
using Enigma5.App.Common.Utils;
using Enigma5.App.Data;
using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Resources.Handlers;

public class SingleThreadDbWriter(
    SimpleSingleThreadRunner simpleSingleThreadRunner,
    IServiceScopeFactory scopeFactory,
    ILogger<SingleThreadDbWriter> logger
) : DbWriter
{
    private readonly SimpleSingleThreadRunner _simpleSingleThreadRunner = simpleSingleThreadRunner;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private readonly ILogger _logger = logger;

    public override async Task<int> RunCreatePeerAsync(Peer peer)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        dbContext.Peers.Add(peer);
        return dbContext.SaveChanges();
    }, _logger);

    public override async Task<int> RunRemoveFileAsync(FileRecord fileRecord)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        dbContext.Remove(fileRecord);
        return dbContext.SaveChanges();
    }, _logger);

    public override async Task<int> RunCreatePendingMessageAsync(PendingMessage pendingMessage)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        dbContext.Messages.Add(pendingMessage);
        return dbContext.SaveChanges();
    }, _logger);

    public override async Task<int> RunRemoveMessagesAsync(Expression<Func<PendingMessage, bool>> predicate)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        return dbContext.Messages.Where(predicate).ExecuteDelete();
    }, _logger);

    public override async Task<int> RunRemoveSharedDataAsync(Expression<Func<SharedData, bool>> predicate)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        return dbContext.SharedData.Where(predicate).ExecuteDelete();
    }, _logger);

    public override async Task<int> RunCreateFileAsync(FileRecord fileRecord)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        dbContext.Files.Add(fileRecord);
        return dbContext.SaveChanges();
    }, _logger);

    public override async Task<int> RunCreateSharedDataAsync(SharedData sharedData)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        dbContext.SharedData.Add(sharedData);
        return dbContext.SaveChanges();
    }, _logger);

    public override async Task<int> RunIncrementFileAccessCountAsync(FileRecord fileRecord)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        fileRecord.AccessCount += 1;
        if (fileRecord.AccessCount >= fileRecord.MaxAccessCount)
        {
            dbContext.Files.Remove(fileRecord);
        }
        else
        {
            dbContext.Files.Update(fileRecord);
        }

        return dbContext.SaveChanges();
    }, _logger);

    public override async Task<int> RunIncrementSharedDataAccessCountAsync(SharedData sharedData)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        sharedData.AccessCount += 1;
        if (sharedData.AccessCount >= sharedData.MaxAccessCount)
        {
            dbContext.SharedData.Remove(sharedData);
        }
        else
        {
            dbContext.SharedData.Update(sharedData);
        }

        return dbContext.SaveChanges();
    }, _logger);

    public override async Task<int> RunMarkMessagesAsDeliveredAsync(Expression<Func<PendingMessage, bool>> predicate)
    {
        var now = DateTimeOffset.UtcNow;
        var utcTimestamp = now.ToUnixTimeSeconds();
        return await _simpleSingleThreadRunner.RunAsync(() =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
            return dbContext.Messages
            .Where(predicate)
            .ExecuteUpdate(s =>
                s.SetProperty(m => m.Sent, true)
                .SetProperty(m => m.DateSent, now)
                .SetProperty(m => m.SentTimestamp, utcTimestamp));
        }, _logger);
    }

    public override async Task<int> RunRemovePeerAsync(Peer peer)
    => await _simpleSingleThreadRunner.RunAsync(() =>
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnigmaDbContext>();
        dbContext.Remove(peer);
        return dbContext.SaveChanges();
    }, _logger);
}
