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

using Enigma5.App.Hangfire;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Enigma5.App.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection SetupHangfire(this IServiceCollection services)
    {
        services.AddHangfire(configuration =>
                {
                    configuration.UseInMemoryStorage();
                    configuration.UseSerializerSettings(new JsonSerializerSettings {
                        TypeNameHandling = TypeNameHandling.All
                    });
                });
        services.AddHangfireServer();

        return services;
    }

    public static IGlobalConfiguration<HangfireActivator> UseAsHangfireActivator(this IServiceProvider serviceProvider)
    {
        return GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(serviceProvider));
    } 
}
