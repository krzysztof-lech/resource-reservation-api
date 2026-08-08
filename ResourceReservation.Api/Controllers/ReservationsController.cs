using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Services.Interfaces;
using System.Security.Claims;

namespace ResourceReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService) =>
        _reservationService = reservationService;

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<IReservationReadDto>))]
    public async Task<ActionResult<IEnumerable<IReservationReadDto>>> GetAll(
        [FromQuery] Guid? userId,
        [FromQuery] string? status,
        [FromQuery] bool? isPast)
    {
        var reservations = await _reservationService.SearchAsync(User.IsInRole("Admin"), userId, status, isPast);
        return Ok(reservations);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReservationReadDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReservationReadDto>> GetById(Guid id)
    {
        var reservation = await _reservationService.GetByIdAsync(id, User.IsInRole("Admin"));
        if (reservation is null) return NotFound();
        return Ok(reservation);
    }

    [HttpGet("user/my")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<IReservationReadDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<IReservationReadDto>>> GetMyReservations(
        [FromQuery] string? status,
        [FromQuery] bool? isPast)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var reservations = await _reservationService.SearchAsync(false, userId, status, isPast);
        return Ok(reservations);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IReservationReadDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReservationReadDto>> Create([FromBody] CreateReservationDto dto)
    {
        if (dto is null) return BadRequest();

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var result = await _reservationService.CreateAsync(dto, userId);
        if (result is null)
            return BadRequest("Resource is unavailable or time range is invalid.");

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var requesterId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var result = await _reservationService.CancelAsync(id, requesterId, isAdmin);

        return result switch
        {
            CancelReservationResult.Success => NoContent(),
            CancelReservationResult.NotFound => NotFound("Reservation not found."),
            CancelReservationResult.Forbidden => Forbid(),
            CancelReservationResult.CannotCancel => BadRequest("Reservation cannot be cancelled."),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
