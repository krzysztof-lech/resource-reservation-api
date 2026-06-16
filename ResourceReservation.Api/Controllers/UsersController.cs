using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Security;

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
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync();

        users.ForEach(u => u.PasswordHash = string.Empty);

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(Guid id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return NotFound();

        user.PasswordHash = string.Empty;
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        if (user is null)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return BadRequest("Password is required.");

        var exists = await _context.Users.AnyAsync(u => u.Email == user.Email);
        if (exists)
            return Conflict("A user with the same email already exists.");

        user.Id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id;
        user.CreatedAt = DateTime.UtcNow;

        user.PasswordHash = PasswordHasher.IsHashedFormat(user.PasswordHash)
            ? user.PasswordHash
            : PasswordHasher.HashPassword(user.PasswordHash);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.Entry(user).State = EntityState.Detached;

        user.PasswordHash = string.Empty;
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User user)
    {
        if (user is null || id != user.Id)
            return BadRequest();

        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
            return NotFound();

        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.Email = user.Email;
        existing.Role = user.Role;

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            existing.PasswordHash = PasswordHasher.IsHashedFormat(user.PasswordHash)
                ? user.PasswordHash
                : PasswordHasher.HashPassword(user.PasswordHash);
        }

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

