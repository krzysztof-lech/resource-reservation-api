using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Models;

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
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var categories = await _context.Categories
            .Include(c => c.Resources)
            .AsNoTracking()
            .ToListAsync();

        foreach (var c in categories)
        {
            foreach (var r in c.Resources)
            {
                r.Category = null;
            }
        }

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Resources)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        foreach (var r in category.Resources)
            r.Category = null;

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory([FromBody] Category category)
    {
        if (category is null)
            return BadRequest();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        category.Resources = new List<Resource>();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
    {
        if (category is null || id != category.Id)
            return BadRequest();

        var existing = await _context.Categories.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.Name = category.Name;
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
