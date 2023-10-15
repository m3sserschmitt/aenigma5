using Microsoft.EntityFrameworkCore;

namespace Enigma5.App.Data;

public class EnigmaDbContext : DbContext
{
#pragma warning disable CS8618
    public EnigmaDbContext(DbContextOptions options) : base(options)
    {
    }
#pragma warning restore CS8618

    public DbSet<PendingMessage> Messages { get; set; }
}
