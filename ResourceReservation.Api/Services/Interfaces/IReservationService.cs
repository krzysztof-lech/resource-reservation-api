using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Services.Interfaces;

public interface IReservationService
{
    Task<IEnumerable<ReservationReadDto>> GetAllAsync();
    Task<ReservationReadDto?> GetByIdAsync(Guid id);
    Task<ReservationReadDto?> CreateAsync(CreateReservationDto dto, Guid userId);
    Task<bool> CancelAsync(Guid id);
    Task<IEnumerable<ReservationReadDto>> GetByUserIdAsync(Guid userId);
}