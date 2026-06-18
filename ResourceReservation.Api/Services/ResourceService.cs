using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services;

public class ResourceService : IResourceService
{
    private readonly AppDbContext _db;
    public ResourceService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ResourceReadDto>> GetAllAsync()
    {
        var resources = await _db.Resources
            .Include(r => r.Category)
            .AsNoTracking()
            .ToListAsync();

        return resources.Select(r => r.ToReadDto());
    }

    public async Task<ResourceReadDto?> GetByIdAsync(Guid id)
    {
        var resource = await _db.Resources
            .Include(r => r.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return resource?.ToReadDto();
    }

    public async Task<ResourceReadDto> CreateAsync(ResourceCreateDto dto)
    {
        var resource = dto.ToEntity();

        resource.Id = resource.Id == Guid.Empty ? Guid.NewGuid() : resource.Id;

        if (resource.AllowedDays != null && resource.AllowedDays.Any())
        {
            resource.AllowedDaysRaw = string.Join(",", resource.AllowedDays);
        }

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();

        return resource.ToReadDto();
    }

    public async Task<bool> UpdateAsync(Guid id, ResourceUpdateDto dto)
    {
        var existing = await _db.Resources.FindAsync(id);
        if (existing is null) return false;

        existing.ApplyUpdates(dto);

        if (existing.AllowedDays != null && existing.AllowedDays.Any())
        {
            existing.AllowedDaysRaw = string.Join(",", existing.AllowedDays);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _db.Resources.FindAsync(id);
        if (existing is null) return false;

        _db.Resources.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
