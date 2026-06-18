using ResourceReservation.Api.Dtos;
namespace ResourceReservation.Api.Services.Interfaces;

public interface IResourceService
{
    Task<IEnumerable<ResourceReadDto>> GetAllAsync();
    Task<ResourceReadDto?> GetByIdAsync(Guid id);
    Task<ResourceReadDto> CreateAsync(ResourceCreateDto dto);
    Task<bool> UpdateAsync(Guid id, ResourceUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
