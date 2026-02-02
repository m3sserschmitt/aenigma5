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
using Enigma5.App.Common.Enums;
using Enigma5.App.Common.Extensions;
using Enigma5.App.Data;
using Enigma5.App.Resources.Handlers;
using Enigma5.Security;
using Enigma5.Security.Contracts;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Enigma5.App.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection SetupHangfire(this IServiceCollection services)
    {
        services.AddHangfire(configuration =>
                {
                    configuration.UseInMemoryStorage();
                    configuration.UseSerializerSettings(new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });
                });
        return services.AddHangfireServer();
    }

    public static IServiceCollection SetupMediatR(this IServiceCollection services)
    => services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(typeof(StartupConfiguration).Assembly);
            config.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
        });

    public static IServiceCollection SetupDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DbConnectionString");
        return services.AddDbContext<EnigmaDbContext>(options =>
        {
            options.UseSqlite(connectionString!);
        });
    }

    public static IServiceCollection SetupKeyReader(this IServiceCollection services, IConfiguration configuration)
    => configuration.GetKeySource() switch
    {
        KeySource.File => services.AddTransient(typeof(IKeyReader), typeof(FileKeyReader)),
        KeySource.Azure => services.AddTransient(typeof(IKeyReader), typeof(AzureKeysReader)),
        _ => services,
    };

    public static IServiceCollection SetupPassphraseReader(this IServiceCollection services, IConfiguration configuration)
    => configuration.GetPassphraseSource() switch
    {
        PassphraseSource.Azure => services.AddTransient(typeof(IPassphraseProvider), typeof(AzurePassphraseReader)),
        PassphraseSource.Dashboard => services.AddTransient(typeof(IPassphraseProvider), typeof(DummyPassphraseProvider)),
        PassphraseSource.Keyboard => services.AddTransient(typeof(IPassphraseProvider), typeof(CommandLinePassphraseReader)),
        _ => services
    };
}
