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
    Task<IEnumerable<ReservationReadDto>> GetAllAsync();
    Task<ReservationReadDto?> GetByIdAsync(Guid id);
    Task<ReservationReadDto?> CreateAsync(CreateReservationDto dto, Guid userId);
    Task<CancelReservationResult> CancelAsync(Guid id, Guid requesterId, bool isAdmin);
    Task<IEnumerable<ReservationReadDto>> GetByUserIdAsync(Guid userId);
}