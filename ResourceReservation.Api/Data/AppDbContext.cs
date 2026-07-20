using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Models;

namespace ResourceReservation.Api.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Reservation>()
            .Property(r => r.Status)
            .HasConversion(
                status => status.DisplayName,
                value => ConvertToReservationStatus(value)
            );
    }

    private static ReservationStatus ConvertToReservationStatus(string value)
    {
        switch (value)
        {
            case "Confirmed":
                return new ConfirmedStatus();
            case "Cancelled":
                return new CancelledStatus();
            default:
                return new PendingStatus();
        }
    }
}



