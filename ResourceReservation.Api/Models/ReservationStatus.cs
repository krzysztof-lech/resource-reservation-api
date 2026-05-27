namespace ResourceReservation.Api.Models;
public abstract class ReservationStatus
{
    public abstract string DisplayName { get; }
    public abstract bool CanTransitionTo(ReservationStatus nextStatus);
}

public class PendingStatus : ReservationStatus
{
    public override string DisplayName => "Pending";
    public override bool CanTransitionTo(ReservationStatus nextStatus) => nextStatus is ConfirmedStatus || nextStatus is CancelledStatus;
}

public class ConfirmedStatus : ReservationStatus
{
    public override string DisplayName => "Confirmed";
    public override bool CanTransitionTo(ReservationStatus nextStatus) => nextStatus is CancelledStatus;
}

public class CancelledStatus : ReservationStatus
{
    public override string DisplayName => "Cancelled";
    public override bool CanTransitionTo(ReservationStatus nextStatus) => false; 
}

