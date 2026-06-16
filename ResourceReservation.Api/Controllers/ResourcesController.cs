using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ResourcesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceReadDto>>> GetResources()
    {
        var resources = await _context.Resources
            .Include(r => r.Category)
            .AsNoTracking()
            .ToListAsync();

        var dtos = resources.Select(r => r.ToReadDto());
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResourceReadDto>> GetResource(Guid id)
    {
        var resource = await _context.Resources
            .Include(r => r.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource is null)
            return NotFound();

        return Ok(resource.ToReadDto());
    }

    [HttpPost]
    public async Task<ActionResult<ResourceReadDto>> CreateResource(ResourceCreateDto dto)
    {
        if (dto == null)
            return BadRequest();

        var resource = dto.ToEntity();
        resource.Id = resource.Id == Guid.Empty ? Guid.NewGuid() : resource.Id;

        if (resource.AllowedDays != null && resource.AllowedDays.Any())
        {
            resource.AllowedDaysRaw = string.Join(",", resource.AllowedDays);
        }

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, resource.ToReadDto());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource(Guid id, [FromBody] ResourceUpdateDto dto)
    {
        if (dto == null)
            return BadRequest();

        var existing = await _context.Resources.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.ApplyUpdates(dto);

        if (existing.AllowedDays != null && existing.AllowedDays.Any())
        {
            existing.AllowedDaysRaw = string.Join(",", existing.AllowedDays);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource(Guid id)
    {
        var existing = await _context.Resources.FindAsync(id);
        if (existing is null)
            return NotFound();

        _context.Resources.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

