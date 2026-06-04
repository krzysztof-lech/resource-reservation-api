using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Models;

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
    public async Task<ActionResult<IEnumerable<Resource>>> GetResources()
    {
        var resources = await _context.Resources
            .Include(r => r.Category)
            .ToListAsync();

        return Ok(resources);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Resource>> GetResource(Guid id)
    {
        var resource = await _context.Resources
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource is null)
            return NotFound();

        return Ok(resource);
    }

    [HttpPost]
    public async Task<ActionResult<Resource>> CreateResource(Resource resource)
    {
        if (resource == null)
            return BadRequest();

        resource.Id = resource.Id == Guid.Empty ? Guid.NewGuid() : resource.Id;

        if (resource.AllowedDays != null && resource.AllowedDays.Any())
        {
            resource.AllowedDaysRaw = string.Join(",", resource.AllowedDays);
        }

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, resource);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource(Guid id, Resource resource)
    {
        if (resource == null || id != resource.Id)
            return BadRequest();

        var existing = await _context.Resources.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.Name = resource.Name;
        existing.Description = resource.Description;
        existing.IsAvailable = resource.IsAvailable;
        existing.SlotDurationMinutes = resource.SlotDurationMinutes;
        existing.AvailableFrom = resource.AvailableFrom;
        existing.AvailableTo = resource.AvailableTo;
        existing.CategoryId = resource.CategoryId;

        if (resource.AllowedDays != null)
        {
            existing.AllowedDays = resource.AllowedDays;
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

