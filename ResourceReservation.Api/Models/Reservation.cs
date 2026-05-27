namespace ResourceReservation.Api.Models;

public class Reservation
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus Status { get; set; } = new PendingStatus();
}

