using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Outrally.Core;

namespace Outrally.Infrastructure;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<ActivityAttendee> ActivityAttendees => Set<ActivityAttendee>();
    public DbSet<CarpoolOffer> CarpoolOffers => Set<CarpoolOffer>();
    public DbSet<CarpoolPassenger> CarpoolPassengers => Set<CarpoolPassenger>();    
}
