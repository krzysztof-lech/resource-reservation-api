using ResourceReservation.Api.Dtos;
namespace ResourceReservation.Api.Services.Interfaces;

public interface IResourceService
{
    Task<IEnumerable<ResourceReadDto>> SearchAsync(
        string? q = null,
        int? categoryId = null,
        bool? isAvailable = null,
        DayOfWeek? day = null,
        TimeOnly? atTime = null);
    Task<ResourceReadDto?> GetByIdAsync(Guid id);
    Task<ResourceReadDto> CreateAsync(ResourceCreateDto dto);
    Task<bool> UpdateAsync(Guid id, ResourceUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
}
