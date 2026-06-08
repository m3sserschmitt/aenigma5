/*
    Aenigma - Federated messaging system
    Copyright © 2024-2026 Romulus-Emanuel Ruja <romulus-emanuel.ruja@tutanota.com>

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

    Task<int> IncrementFileAccessCountAsync(FileRecord fileRecord, CancellationToken cancellationToken = default);

    Task<int> CreatePendingMessageAsync(PendingMessage pendingMessage, CancellationToken cancellationToken = default);

    Task<int> RemoveMessagesAsync(Expression<Func<PendingMessage, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> MarkMessagesAsDeliveredAsync(Expression<Func<PendingMessage, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> CreateSharedDataAsync(SharedData sharedData, CancellationToken cancellationToken = default);

    Task<int> RemoveSharedDataAsync(Expression<Func<SharedData, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> IncrementSharedDataAccessCountAsync(SharedData sharedData, CancellationToken cancellationToken = default);
}
