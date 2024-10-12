/*
    Aenigma - Onion Routing based messaging application
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

using Enigma5.App.Common.Extensions;

namespace NetworkBridge;

public class Program
{
    private static readonly string _configFile = "appsettings.json";

    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private static readonly ConfigurationLoader _configurationLoader = new(_configFile);

    private static readonly HubConnectionsProxy _connections = HubConnectionsProxy.Create(_configurationLoader);

    public static async Task Main()
    {
        RegisterEvents();

        await StartAsync();
        Console.WriteLine("Connection successfully completed.");
        await BlockAsync();
    }

    private static void RegisterEvents()
    {
        _connections.OnAnyTargetClosed += OnConnectionClosedAsync;
        _connections.OnConnectionsReloaded += async () => await OnConnectionClosedAsync(null);
    }

    private static async Task StartAsync()
    {
        if (!await _connections.StartAsync())
        {
            throw new Exception("Some peers could not be connected.");
        }

        if (!await _connections.StartAuthenticationAsync())
        {
            throw new Exception("Authentication failed on some peers.");
        }

        if (!await _connections.TriggerBroadcast())
        {
            throw new Exception("Failed to trigger broadcast.");
        }
    }

    private static async Task OnConnectionClosedAsync(Exception? ex)
    {
        // TODO: log exception
        await _semaphoreSlim.WaitAsync();

        for (int i = 0; i < _configurationLoader.Configuration.GetConnectionRetriesCount(); i++)
        {
            await Task.Delay(_configurationLoader.Configuration.GetDelayBetweenConnectionRetries());
            try
            {
                await StartAsync();
                Console.WriteLine("Connections established");
                break;
            }
            catch (Exception)
            {
                // TODO: Log failed attempt
                Console.WriteLine("Failed attempt to reestablish connection.");
                continue;
            }
        }

        _semaphoreSlim.Release();
    }

    private static async Task BlockAsync()
    {
        while (true) await Task.Delay(int.MaxValue);
    }
}
