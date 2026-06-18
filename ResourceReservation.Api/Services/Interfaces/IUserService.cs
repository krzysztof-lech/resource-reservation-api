using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserReadDto>> GetAllAsync();
    Task<UserReadDto?> GetByIdAsync(Guid id);
    Task<UserReadDto?> CreateAsync(UserCreateDto dto);
    Task<bool> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
