namespace ResourceReservation.Api.Dtos;

public interface IReservationReadDto
{
    Guid Id { get; init; }
    Guid ResourceId { get; init; }
    string ResourceName { get; init; }
    DateTime StartTime { get; init; }
    DateTime EndTime { get; init; }
    string Status { get; init; }
}
