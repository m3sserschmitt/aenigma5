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
using Enigma5.App.Data;
using Enigma5.App.Resources.Contracts;

namespace Enigma5.App.Resources.Handlers;

public abstract class DbWriter : IDbWriter
{
    public abstract Task<int> RunCreatePeerAsync(Peer peer);

    public abstract Task<int> RunRemovePeerAsync(Peer peer);

    public abstract Task<int> RunRemoveFileAsync(FileRecord fileRecord);

    public abstract Task<PendingMessage?> RunCreatePendingMessageAsync(PendingMessage pendingMessage, bool skipIfUuidExists);

    public abstract Task<int> RunRemoveMessagesAsync(Expression<Func<PendingMessage, bool>> predicate);

    public abstract Task<int> RunMarkMessagesAsDeliveredAsync(Expression<Func<PendingMessage, bool>> predicate);

    public abstract Task<int> RunRemoveSharedDataAsync(Expression<Func<SharedData, bool>> predicate);

    public abstract Task<int> RunCreateFileAsync(FileRecord fileRecord);

    public abstract Task<int> RunCreateSharedDataAsync(SharedData sharedData);

    public abstract Task<FileRecord?> RunIncrementFileAccessCountAsync(string tag);

    public abstract Task<SharedData?> RunIncrementSharedDataAccessCountAsync(string tag);

    public async Task<int> CreatePeerAsync(Peer peer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunCreatePeerAsync(peer);
    }

    public async Task<int> RemoveFileAsync(FileRecord fileRecord, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunRemoveFileAsync(fileRecord);
    }

    public async Task<int> RemoveMessagesAsync(Expression<Func<PendingMessage, bool>> predicate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunRemoveMessagesAsync(predicate);
    }

    public async Task<int> RemoveSharedDataAsync(Expression<Func<SharedData, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunRemoveSharedDataAsync(predicate);
    }

    public async Task<int> CreateFileAsync(FileRecord fileRecord, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunCreateFileAsync(fileRecord);
    }

    public async Task<int> CreateSharedDataAsync(SharedData sharedData, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunCreateSharedDataAsync(sharedData);
    }

    public async Task<PendingMessage?> CreatePendingMessageAsync(PendingMessage pendingMessage, bool skipIfUuidExists, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunCreatePendingMessageAsync(pendingMessage, skipIfUuidExists);
    }

    public async Task<FileRecord?> IncrementFileAccessCountAsync(string tag, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunIncrementFileAccessCountAsync(tag);
    }

    public async Task<SharedData?> IncrementSharedDataAccessCountAsync(string tag, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunIncrementSharedDataAccessCountAsync(tag);
    }

    public async Task<int> MarkMessagesAsDeliveredAsync(Expression<Func<PendingMessage, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunMarkMessagesAsDeliveredAsync(predicate);
    }

    public async Task<int> RemovePeerAsync(Peer peer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await RunRemovePeerAsync(peer);
    }
}
