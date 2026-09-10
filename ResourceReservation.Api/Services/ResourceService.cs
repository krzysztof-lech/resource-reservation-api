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

    public async Task<IEnumerable<ResourceReadDto>> SearchAsync(string? q = null, int? categoryId = null, bool? isAvailable = null, DayOfWeek? day = null, TimeOnly? atTime = null)
    {
        _logger.LogInformation("Searching resources q='{Q}', categoryId={CategoryId}, isAvailable={IsAvailable}, day={Day}, atTime={AtTime}", q, categoryId, isAvailable, day, atTime);

        var query = _db.Resources
            .Include(r => r.Category)
            .Include(r => r.Images)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(r =>
                r.Name.Contains(normalized) ||
                (r.Description != null && r.Description.Contains(normalized))
            );
        }

        if (categoryId.HasValue)
            query = query.Where(r => r.CategoryId == categoryId.Value);

        if (isAvailable.HasValue)
            query = query.Where(r => r.IsAvailable == isAvailable.Value);

        if (day.HasValue)
        {
            var dayName = day.Value.ToString();
            query = query.Where(r => r.AllowedDaysRaw.Contains(dayName));
        }

        if (atTime.HasValue)
        {
            var t = atTime.Value;
            query = query.Where(r => r.AvailableFrom <= t && r.AvailableTo >= t);
        }

        var results = await query.ToListAsync();
        _logger.LogInformation("Search returned {Count} resources", results.Count);
        return results.Select(r => r.ToReadDto());
    }

    public async Task<ResourceReadDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching resource {ResourceId}", id);
        var resource = await _db.Resources
            .Include(r => r.Category)
            .Include(r => r.Images)
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

    public async Task<ResourceImageDto?> AddImageAsync(Guid resourceId, string fileName)
    {
        _logger.LogInformation("Adding image {FileName} to resource {ResourceId}", fileName, resourceId);

        var resourceExists = await _db.Resources.AnyAsync(r => r.Id == resourceId);
        if (!resourceExists)
        {
            _logger.LogWarning("Resource {ResourceId} not found for image upload", resourceId);
            return null;
        }

        var maxOrder = await _db.Images
            .Where(i => i.ResourceId == resourceId)
            .Select(i => (int?)i.DisplayOrder)
            .MaxAsync() ?? -1;

        var image = new Models.ResourceImage
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            FileName = fileName,
            DisplayOrder = maxOrder + 1
        };

        _db.Images.Add(image);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Image {ImageId} added to resource {ResourceId}", image.Id, resourceId);

        return new ResourceImageDto
        {
            Id = image.Id,
            Url = $"/uploads/resources/{image.FileName}",
            DisplayOrder = image.DisplayOrder
        };
    }

    public async Task<bool> DeleteImageAsync(Guid resourceId, Guid imageId)
    {
        _logger.LogInformation("Deleting image {ImageId} from resource {ResourceId}", imageId, resourceId);

        var image = await _db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.ResourceId == resourceId);
        if (image is null)
        {
            _logger.LogWarning("Image {ImageId} not found for resource {ResourceId}", imageId, resourceId);
            return false;
        }

        _db.Images.Remove(image);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Image {ImageId} deleted successfully", imageId);
        return true;
    }
}
