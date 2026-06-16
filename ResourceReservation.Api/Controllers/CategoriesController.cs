using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryReadDto>>> GetCategories()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .ToListAsync();

        var dtos = categories.Select(c => c.ToReadDto());
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryReadDto>> GetCategory(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        return Ok(category.ToReadDto());
    }

    [HttpPost]
    public async Task<ActionResult<CategoryReadDto>> CreateCategory([FromBody] CategoryCreateDto dto)
    {
        if (dto is null)
            return BadRequest();

        var category = dto.ToEntity();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category.ToReadDto());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryUpdateDto dto)
    {
        if (dto is null)
            return BadRequest();

        var existing = await _context.Categories.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.ApplyUpdates(dto);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var existing = await _context.Categories.FindAsync(id);
        if (existing is null)
            return NotFound();

        _context.Categories.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
