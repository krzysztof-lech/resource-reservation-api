using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Services.Interfaces;

public enum CancelReservationResult
{
    Success,
    NotFound,
    Forbidden,
    CannotCancel
}
public interface IReservationService
{
    Task<IEnumerable<ReservationReadDto>> SearchAsync(
        Guid? userId = null,
        string? status = null,
        bool? isPast = null
    );
    Task<ReservationReadDto?> GetByIdAsync(Guid id);
    Task<ReservationReadDto?> CreateAsync(CreateReservationDto dto, Guid userId);
    Task<CancelReservationResult> CancelAsync(Guid id, Guid requesterId, bool isAdmin);
}