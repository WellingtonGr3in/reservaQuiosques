using Microsoft.EntityFrameworkCore;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence;

public class QuiosquesDbContext(DbContextOptions<QuiosquesDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Kiosk> Kiosks => Set<Kiosk>();
    public DbSet<KioskPhoto> KioskPhotos => Set<KioskPhoto>();
    public DbSet<KioskItem> KioskItems => Set<KioskItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Administrator> Administrators => Set<Administrator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuiosquesDbContext).Assembly);
    }
}
