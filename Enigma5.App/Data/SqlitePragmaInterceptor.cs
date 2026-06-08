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

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Enigma5.App.Data;

public class SqlitePragmaInterceptor(ILogger<SqlitePragmaInterceptor> logger) : DbConnectionInterceptor
{
    private readonly ILogger _logger = logger;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        try
        {
            ApplyPragmas(connection);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error while applying database pragmas.");
            throw;
        }
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ApplyPragmasAsync(connection, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error while applying database pragmas.");
            throw;
        }
    }

    private static void ApplyPragmas(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        var result = cmd.ExecuteScalar()?.ToString();
        if (result != "wal")
        {
            throw new InvalidOperationException($"Failed to set WAL mode, got: {result}");
        }
        foreach (var pragma in GetPragmas())
        {
            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = pragma;
            pragmaCmd.ExecuteNonQuery();
        }
    }

    private static async Task ApplyPragmasAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        var result = (await cmd.ExecuteScalarAsync(cancellationToken))?.ToString();
        if (result != "wal")
        {
            throw new InvalidOperationException($"Failed to set WAL mode, got: {result}");
        }
        foreach (var pragma in GetPragmas())
        {
            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = pragma;
            await pragmaCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static IEnumerable<string> GetPragmas() =>
    [
        "PRAGMA synchronous=NORMAL;",
        "PRAGMA foreign_keys=ON;"
    ];
}
