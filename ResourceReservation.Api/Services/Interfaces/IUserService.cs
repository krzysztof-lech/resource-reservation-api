using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserReadDto>> SearchAsync(
        string? q = null,
        string? role = null,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null,
        int? page = null,
        int? pageSize = null
    );
    Task<UserReadDto?> GetByIdAsync(Guid id);
    Task<UserReadDto?> CreateAsync(UserCreateDto dto);
    Task<bool> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
