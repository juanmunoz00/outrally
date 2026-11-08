using Microsoft.EntityFrameworkCore;
using Outrally.Core;

namespace Outrally.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Activity> Activities => Set<Activity>();
}
