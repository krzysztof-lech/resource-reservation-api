using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services;

public class ResourceService : IResourceService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ResourceService> _logger;
    public ResourceService(AppDbContext db, ILogger<ResourceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<ResourceReadDto>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all resources");
        var resources = await _db.Resources
            .Include(r => r.Category)
            .AsNoTracking()
            .ToListAsync();

        _logger.LogInformation("Found {Count} resources", resources.Count);
        return resources.Select(r => r.ToReadDto());
    }

    public async Task<ResourceReadDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching resource {ResourceId}", id);
        var resource = await _db.Resources
            .Include(r => r.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource is null)
            _logger.LogWarning("Resource {ResourceId} not found", id);

        return resource?.ToReadDto();
    }

    public async Task<ResourceReadDto> CreateAsync(ResourceCreateDto dto)
    {
        _logger.LogInformation("Creating resource {ResourceName}", dto.Name);
        var resource = dto.ToEntity();

        resource.Id = resource.Id == Guid.Empty ? Guid.NewGuid() : resource.Id;

        if (resource.AllowedDays != null && resource.AllowedDays.Any())
        {
            resource.AllowedDaysRaw = string.Join(",", resource.AllowedDays);
        }

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Resource {ResourceId} created successfully", resource.Id);

        return resource.ToReadDto();
    }

    public async Task<bool> UpdateAsync(Guid id, ResourceUpdateDto dto)
    {
        _logger.LogInformation("Updating resource {ResourceId}", id);
        var existing = await _db.Resources.FindAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Resource {ResourceId} not found for update", id);
            return false;
        }

        existing.ApplyUpdates(dto);

        if (existing.AllowedDays != null && existing.AllowedDays.Any())
        {
            existing.AllowedDaysRaw = string.Join(",", existing.AllowedDays);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Resource {ResourceId} updated successfully", id);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting resource {ResourceId}", id);
        var existing = await _db.Resources.FindAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Resource {ResourceId} not found for deletion", id);
            return false;
        }

        _db.Resources.Remove(existing);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Resource {ResourceId} deleted successfully", id);
        return true;
    }
}
