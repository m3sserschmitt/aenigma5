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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Enigma5.Tests.Base;

namespace Enigma5.App.IntegrationTests;

// A real node, started as a process from the build output of Enigma5.App, with its own folder for the
// database, the key files and the uploads, and with free ports on the loopback address.
public sealed class NodeProcess : IAsyncDisposable
{
    private readonly TempFolder _folder = new();

    private readonly ProcessStartInfo _start;

    private Process _process;

    private readonly ConcurrentQueue<string> _output = new();

    public string PublicUrl { get; }

    public string ControlUrl { get; }

    public string Address { get; }

    public string PublicKey { get; }

    public HttpClient Public { get; }

    public HttpClient Control { get; }

    public string DatabasePath => _folder.File("node.sqlite");

    public string UploadFolder => _folder.File("uploads");

    // Everything the node has written to its console so far.
    public IReadOnlyList<string> Output => [.. _output];

    // The log of the node so far, as text. The node keeps the file open, so it is read without taking it.
    public string Log
    {
        get
        {
            if (!File.Exists(LogPath))
            {
                return string.Empty;
            }
            using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    private string LogPath => _folder.File("node.log");

    private NodeProcess(string privateKey, IEnumerable<(string Key, string Value)> settings)
    {
        PublicKey = TestKeys.PublicKeyOf(privateKey);
        Address = Crypto.CertificateHelper.GetHexAddressFromPublicKey(PublicKey);
        PublicUrl = $"http://127.0.0.1:{FreePort()}";
        ControlUrl = $"http://127.0.0.1:{FreePort()}";
        Public = new HttpClient { BaseAddress = new Uri(PublicUrl) };
        Control = new HttpClient { BaseAddress = new Uri(ControlUrl) };
        _folder.WriteFile("private-key.pem", privateKey);
        _folder.WriteFile("public-key.pem", PublicKey);

        (string Key, string Value)[] defaults =
        [
            ("ConnectionStrings:DbConnectionString", $"data source={DatabasePath}"),
            ("Kestrel:EndPoints:Http:Url", PublicUrl),
            ("Kestrel:EndPoints:HttpControl:Url", ControlUrl),
            ("PrivateKeyPath", _folder.File("private-key.pem")),
            ("PublicKeyPath", _folder.File("public-key.pem")),
            ("WebContentDirectory", UploadFolder),
            ("PassphrasePersistence", "Ephemeral"),
            ("Hostname", PublicUrl),
            ("Serilog:MinimumLevel:Default", "Information"),
            ("Serilog:MinimumLevel:Override:Microsoft", "Warning"),
            ("Serilog:WriteTo:0:Name", "File"),
            ("Serilog:WriteTo:0:Args:path", LogPath),
            ("Serilog:WriteTo:0:Args:shared", "true"),
            // The shipped rules: the dashboard and the hub method TriggerBroadcast are closed on the public endpoint.
            ("HttpBlacklists:0:Endpoint", PublicUrl),
            ("HttpBlacklists:0:Items:0:Path", "/Dashboard"),
            ("HttpBlacklists:0:Items:0:Methods:0", "GET"),
            ("HttpBlacklists:0:Items:1:Path", "/_blazor"),
            ("HttpBlacklists:0:Items:1:Methods:0", "GET"),
            ("HttpBlacklists:0:Items:1:Methods:1", "POST"),
            ("HubBlacklists:0:Endpoint", PublicUrl),
            ("HubBlacklists:0:Items:0:Methods:0", "TriggerBroadcast")
        ];
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _folder.Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(NodeAssembly());
        // Later settings win, so a test can replace a default.
        foreach (var (key, value) in defaults.Concat(settings))
        {
            start.ArgumentList.Add($"--{key}={value}");
        }
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        _start = start;
        _process = NewProcess();
    }

    private Process NewProcess()
    {
        var process = new Process { StartInfo = _start };
        process.OutputDataReceived += (_, line) => { if (line.Data is not null) _output.Enqueue(line.Data); };
        process.ErrorDataReceived += (_, line) => { if (line.Data is not null) _output.Enqueue(line.Data); };
        return process;
    }

    // Node A of the tests has key 1, node B has key 2.
    public static Task<NodeProcess> StartAAsync(params (string Key, string Value)[] settings) => StartAsync(TestKeys.PlainPrivateKey1, settings);

    public static Task<NodeProcess> StartBAsync(params (string Key, string Value)[] settings) => StartAsync(TestKeys.PlainPrivateKey2, settings);

    public static async Task<NodeProcess> StartAsync(string privateKey, params (string Key, string Value)[] settings)
    {
        // Another program may take a port between the moment it is found free and the moment the node listens on it.
        for (var attempt = 1; ; attempt++)
        {
            var node = new NodeProcess(privateKey, settings);
            try
            {
                await node.RunAsync();
                return node;
            }
            catch (InvalidOperationException) when (attempt < 3 && node.Output.Any(line => line.Contains("address already in use")))
            {
            }
        }
    }

    private async Task RunAsync()
    {
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        await WaitUntilReadyAsync();
    }

    // Ends the node at once and starts it again with the same folder, settings and ports.
    public async Task RestartAsync()
    {
        await EndAsync();
        _process = NewProcess();
        await RunAsync();
    }

    private async Task EndAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                // A node with open connections takes its time to stop, so it is ended at once.
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch (InvalidOperationException)
        {
            // Never started.
        }
        _process.Dispose();
    }

    private async Task WaitUntilReadyAsync()
    {
        var limit = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < limit)
        {
            if (_process.HasExited)
            {
                break;
            }
            try
            {
                if ((await Public.GetAsync("/Info")).StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            await Task.Delay(100);
        }
        var output = string.Join(Environment.NewLine, Output.TakeLast(30));
        await DisposeAsync();
        throw new InvalidOperationException($"The node did not start. Its last output:{Environment.NewLine}{output}");
    }

    // Reads one value from the node's database, without changing it.
    public long Count(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DatabasePath};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        return Convert.ToInt64(command.ExecuteScalar() ?? 0);
    }

    // Changes the node's database directly, for what only the dashboard can do on a running node.
    public void Execute(string sql, params (string Name, object Value)[] parameters)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        command.ExecuteNonQuery();
    }

    // Stores a peer as the dashboard does. The node connects to it at its next start.
    public void AddPeer(NodeProcess peer) => Execute(
        "INSERT INTO Peers (Host, Address, DateCreated, Timestamp) VALUES ($h, $a, $c, $t)",
        ("$h", peer.PublicUrl), ("$a", peer.Address), ("$c", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz")), ("$t", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

    // Messages stored for a destination that are not confirmed yet.
    public long Pending(string destination) => Count("SELECT COUNT(*) FROM Messages WHERE Destination = $d AND Sent = 0", ("$d", destination));

    // Waits until a condition holds, for what the node does a moment after a call returns.
    public static async Task<bool> Eventually(Func<Task<bool>> condition, int seconds = 20)
    {
        var limit = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < limit)
        {
            if (await condition())
            {
                return true;
            }
            await Task.Delay(100);
        }
        return await condition();
    }

    // Ports already given to a node of this test run. The system may offer a port again once it is released.
    private static readonly HashSet<int> GivenPorts = [];

    private static int FreePort()
    {
        lock (GivenPorts)
        {
            while (true)
            {
                using var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                if (GivenPorts.Add(port))
                {
                    return port;
                }
            }
        }
    }

    // The tests run from <repository>/Enigma5.App.IntegrationTests/bin/<configuration>/<framework>/.
    private static string NodeAssembly()
    {
        var output = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var (framework, configuration, repository) = (output.Name, output.Parent!.Name, output.Parent.Parent!.Parent!.Parent!.FullName);
        var path = Path.Combine(repository, "Enigma5.App", "bin", configuration, framework, "Enigma5.App.dll");
        return File.Exists(path) ? path : throw new FileNotFoundException("The node is not built.", path);
    }

    public async ValueTask DisposeAsync()
    {
        await EndAsync();
        Public.Dispose();
        Control.Dispose();
        _folder.Dispose();
    }
}
