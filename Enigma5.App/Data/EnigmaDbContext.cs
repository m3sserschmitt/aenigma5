using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Data;

public class EnigmaDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<PendingMessage> Messages { get; set; }

    public DbSet<SharedData> SharedData { get; set; }

    public DbSet<AuthorizedService> AuthorizedServices { get; set; }
}
