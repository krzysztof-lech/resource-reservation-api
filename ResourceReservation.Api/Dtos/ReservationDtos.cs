namespace ResourceReservation.Api.Dtos;

public record CreateReservationDto(
    Guid ResourceId,
    DateTime StartTime,
    DateTime EndTime
);

public record ReservationReadDto(
    Guid Id,
    Guid ResourceId,
    string ResourceName,
    Guid UserId,
    string UserEmail,
    DateTime StartTime,
    DateTime EndTime,
    string Status
) : IReservationReadDto;

public record ReservationPublicReadDto(
    Guid Id,
    Guid ResourceId,
    string ResourceName,
    DateTime StartTime,
    DateTime EndTime,
    string Status
) : IReservationReadDto;
