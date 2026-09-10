using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly IWebHostEnvironment _env;


    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public ResourcesController(IResourceService resourceService, IWebHostEnvironment env)
    {
        _resourceService = resourceService;
        _env = env;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ResourceReadDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ResourceReadDto>>> GetResources(
        [FromQuery] string? q,
        [FromQuery] int? categoryId,
        [FromQuery] bool? isAvailable,
        [FromQuery] DayOfWeek? day,
        [FromQuery] string? atTime)
    {
        TimeOnly? parsedTime = null;
        if (!string.IsNullOrWhiteSpace(atTime))
        {
            if (!TimeOnly.TryParse(atTime, out var t))
                return BadRequest("Invalid atTime format. Use HH:mm or HH:mm:ss.");
            parsedTime = t;
        }

        var isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
        if (!isAdmin)
        {
            isAvailable = true;
        }

        var filteredResources = await _resourceService.SearchAsync(q, categoryId, isAvailable, day, parsedTime);
        return Ok(filteredResources);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResourceReadDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceReadDto>> GetResource(Guid id)
    {
        var resource = await _resourceService.GetByIdAsync(id);
        if (resource is null) return NotFound();

        var isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
        if (!resource.IsAvailable && !isAdmin)
        {
            return NotFound();
        }

        return Ok(resource);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResourceReadDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResourceReadDto>> CreateResource(ResourceCreateDto dto)
    {
        if (dto == null)
            return BadRequest();

        var created = await _resourceService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetResource), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResource(Guid id, [FromBody] ResourceUpdateDto dto)
    {
        if (dto == null)
            return BadRequest();

        var ok = await _resourceService.UpdateAsync(id, dto);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResource(Guid id)
    {
        var ok = await _resourceService.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/images")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ResourceImageDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceImageDto>> UploadImage(Guid id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file was uploaded.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("File is too large. Maximum size is 5 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Unsupported file type. Allowed types: jpg, jpeg, png, webp.");

        var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "resources");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var result = await _resourceService.AddImageAsync(id, fileName);
        if (result is null)
        {
            System.IO.File.Delete(filePath);
            return NotFound("Resource not found.");
        }

        return CreatedAtAction(nameof(GetResource), new { id }, result);
    }

    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
    {
        var resource = await _resourceService.GetByIdAsync(id);
        var image = resource?.Images.FirstOrDefault(i => i.Id == imageId);

        var ok = await _resourceService.DeleteImageAsync(id, imageId);
        if (!ok) return NotFound();

        if (image is not null)
        {
            var fileName = Path.GetFileName(image.Url);
            var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRootPath, "uploads", "resources", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        return NoContent();
    }
}

