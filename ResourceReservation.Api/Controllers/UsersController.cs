using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        var dtos = users.Select(u => u.ToReadDto());
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserReadDto>> GetUser(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound();

        return Ok(user.ToReadDto());
    }

    [HttpPost]
    public async Task<ActionResult<UserReadDto>> CreateUser([FromBody] UserCreateDto dto)
    {
        if (dto is null)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Password is required.");

        var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
            return Conflict("A user with the same email already exists.");

        var user = dto.ToEntity();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user.ToReadDto());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
    {
        if (dto is null)
            return BadRequest();

        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.ApplyUpdates(dto);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
            return NotFound();

        _context.Users.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

