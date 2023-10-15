using Enigma5.App.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enigma5.App.Extensions;

public static class DbContextExtensions
{
    public static IServiceCollection SetupDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DbConnectionString");
        services.AddDbContext<EnigmaDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });
        
        return services;
    }
}