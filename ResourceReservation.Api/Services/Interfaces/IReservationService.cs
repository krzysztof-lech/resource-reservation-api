using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Services.Interfaces;

public enum CancelReservationResult
{
    Success,
    NotFound,
    Forbidden,
    CannotCancel
}
public enum ConfirmReservationResult
{
    Success,
    NotFound,
    CannotConfirm
}
public interface IReservationService
{
    Task<IEnumerable<IReservationReadDto>> SearchAsync(
        bool isAdmin,
        Guid? userId = null,
        string? status = null,
        bool? isPast = null
    );
    Task<IReservationReadDto?> GetByIdAsync(Guid id, bool isAdmin);
    Task<IReservationReadDto?> CreateAsync(CreateReservationDto dto, Guid userId);
    Task<CancelReservationResult> CancelAsync(Guid id, Guid requesterId, bool isAdmin);
    Task<ConfirmReservationResult> ConfirmAsync(Guid id);
}