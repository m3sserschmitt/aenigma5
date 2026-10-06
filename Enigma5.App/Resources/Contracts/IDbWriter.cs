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

namespace Enigma5.App.Resources.Contracts;

public interface IDbWriter
{
    Task<int> CreatePeerAsync(Peer peer, CancellationToken cancellationToken = default);

    Task<int> RemovePeerAsync(Peer peer, CancellationToken cancellationToken = default);

    Task<int> CreateFileAsync(FileRecord fileRecord, CancellationToken cancellationToken = default);

    Task<int> RemoveFileAsync(FileRecord fileRecord, CancellationToken cancellationToken = default);

    // Adds one to the access count, or removes the record when the maximum is reached.
    // Returns the record with its new count, or null if the tag is not known.
    Task<FileRecord?> IncrementFileAccessCountAsync(string tag, CancellationToken cancellationToken = default);

    // Returns the stored message: the given one if it was added, or, when skipIfUuidExists is set,
    // the message already stored with the same uuid. Returns null if nothing was stored.
    Task<PendingMessage?> CreatePendingMessageAsync(PendingMessage pendingMessage, bool skipIfUuidExists, CancellationToken cancellationToken = default);

    Task<int> RemoveMessagesAsync(Expression<Func<PendingMessage, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> MarkMessagesAsDeliveredAsync(Expression<Func<PendingMessage, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> CreateSharedDataAsync(SharedData sharedData, CancellationToken cancellationToken = default);

    Task<int> RemoveSharedDataAsync(Expression<Func<SharedData, bool>> predicate, CancellationToken cancellationToken = default);

    // Same contract as IncrementFileAccessCountAsync.
    Task<SharedData?> IncrementSharedDataAccessCountAsync(string tag, CancellationToken cancellationToken = default);
}
