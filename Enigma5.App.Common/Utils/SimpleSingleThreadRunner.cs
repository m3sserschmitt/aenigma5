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

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Enigma5.App.Common.Utils;

public sealed class SimpleSingleThreadRunner : IDisposable
{
    private const string EXCEPTION_MESSAGE = "Exception encountered while doing work on single thread executor";

    readonly Thread _thread;

    readonly BlockingCollection<Func<Task>> _queue = [];

    public SimpleSingleThreadRunner()
    {
        _thread = new Thread(ThreadLoop) { IsBackground = true };
        _thread.Start();
    }

    void ThreadLoop()
    {
        foreach (var work in _queue.GetConsumingEnumerable())
        {
            work().GetAwaiter().GetResult();
        }
    }

    public Task<T> RunAsync<T>(Func<T> work, ILogger? logger = null)
    {
        var tcs = new TaskCompletionSource<T>();
        _queue.Add(() =>
        {
            try { tcs.SetResult(work()); }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                logger?.LogError(ex, EXCEPTION_MESSAGE);
            }
            return Task.CompletedTask;
        });
        return tcs.Task;
    }

    public Task<T> RunAsync<T>(Func<Task<T>> work, ILogger? logger = null)
    {
        var tcs = new TaskCompletionSource<T>();
        _queue.Add(async () =>
        {
            try { tcs.SetResult(await work()); }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                logger?.LogError(ex, EXCEPTION_MESSAGE);
            }
        });
        return tcs.Task;
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();
    }
}
