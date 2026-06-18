using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserReadDto>> GetUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserReadDto>> CreateUser([FromBody] UserCreateDto dto)
    {
        if (dto is null)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Password is required.");

        var created = await _userService.CreateAsync(dto);

        if (created is null)
            return Conflict("A user with the same email already exists.");

        return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
    {
        if (dto is null)
            return BadRequest();

        var ok = await _userService.UpdateAsync(id, dto);
        if (!ok)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var ok = await _userService.DeleteAsync(id);
        if (!ok)
            return NotFound();
        return NoContent();
    }
}

