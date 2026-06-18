using Microsoft.AspNetCore.Mvc;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public ResourcesController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceReadDto>>> GetResources()
    {
        var resources = await _resourceService.GetAllAsync();
        return Ok(resources);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResourceReadDto>> GetResource(Guid id)
    {
        var resource = await _resourceService.GetByIdAsync(id);
        if (resource is null) return NotFound();
        return Ok(resource);
    }

    [HttpPost]
    public async Task<ActionResult<ResourceReadDto>> CreateResource(ResourceCreateDto dto)
    {
        if (dto == null)
            return BadRequest();

        var created = await _resourceService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetResource), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource(Guid id, [FromBody] ResourceUpdateDto dto)
    {
        if (dto == null)
            return BadRequest();

        var ok = await _resourceService.UpdateAsync(id, dto);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource(Guid id)
    {
        var ok = await _resourceService.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}

