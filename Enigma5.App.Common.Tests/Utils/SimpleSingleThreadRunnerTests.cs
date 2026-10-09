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

using Enigma5.App.Common.Utils;

namespace Enigma5.App.Common.Tests.Utils;

public class SimpleSingleThreadRunnerTests
{
    [Fact]
    public async Task RunAsync_returns_the_result_of_the_work()
    {
        using var runner = new SimpleSingleThreadRunner();

        Assert.Equal(42, await runner.RunAsync(() => 42));
        Assert.Equal("done", await runner.RunAsync(async () =>
        {
            await Task.Delay(10);
            return "done";
        }));
    }

    [Fact]
    public async Task Work_is_done_in_the_order_in_which_it_was_given()
    {
        using var runner = new SimpleSingleThreadRunner();
        var done = new List<int>();

        var tasks = Enumerable.Range(0, 200).Select(index => runner.RunAsync(() =>
        {
            done.Add(index);
            return index;
        })).ToList();
        await Task.WhenAll(tasks);

        Assert.Equal(Enumerable.Range(0, 200), done);
    }

    [Fact]
    public async Task Work_given_from_many_threads_never_overlaps()
    {
        using var runner = new SimpleSingleThreadRunner();
        var running = 0;
        var largest = 0;
        var total = 0;

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            for (var index = 0; index < 25; index++)
            {
                await runner.RunAsync(async () =>
                {
                    largest = Math.Max(largest, Interlocked.Increment(ref running));
                    await Task.Delay(1);
                    total++;
                    Interlocked.Decrement(ref running);
                    return true;
                });
            }
        })));

        Assert.Equal(1, largest);
        Assert.Equal(200, total);
    }

    [Fact]
    public async Task Asynchronous_work_is_finished_before_the_next_work_starts()
    {
        using var runner = new SimpleSingleThreadRunner();
        var events = new List<string>();

        var first = runner.RunAsync(async () =>
        {
            events.Add("first starts");
            await Task.Delay(100);
            events.Add("first ends");
            return true;
        });
        var second = runner.RunAsync(() =>
        {
            events.Add("second");
            return true;
        });
        await Task.WhenAll(first, second);

        Assert.Equal(["first starts", "first ends", "second"], events);
    }

    [Fact]
    public async Task Synchronous_work_always_runs_on_the_same_thread()
    {
        using var runner = new SimpleSingleThreadRunner();

        var threads = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => runner.RunAsync(() => Environment.CurrentManagedThreadId)));

        Assert.Single(threads.Distinct());
        Assert.NotEqual(Environment.CurrentManagedThreadId, threads[0]);
    }

    [Fact]
    public async Task An_exception_reaches_the_caller()
    {
        using var runner = new SimpleSingleThreadRunner();

        Func<int> failing = () => throw new InvalidOperationException("sync");
        Func<Task<int>> failingLater = async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("async");
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(failing));
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(failingLater));
    }

    [Fact]
    public async Task The_runner_goes_on_after_work_that_failed()
    {
        using var runner = new SimpleSingleThreadRunner();

        Func<int> failing = () => throw new InvalidOperationException();

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(failing));

        Assert.Equal(7, await runner.RunAsync(() => 7));
    }

    [Fact]
    public async Task The_caller_continues_on_another_thread_than_the_runner()
    {
        using var runner = new SimpleSingleThreadRunner();

        var runnerThread = await runner.RunAsync(() => Environment.CurrentManagedThreadId);

        // If the caller continued on the runner thread, work it then gives to the runner and waits for would never start.
        Assert.NotEqual(runnerThread, Environment.CurrentManagedThreadId);
        Assert.Equal(1, await runner.RunAsync(() => 1));
    }
}
