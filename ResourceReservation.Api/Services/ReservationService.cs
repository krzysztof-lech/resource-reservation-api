using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(AppDbContext db, ILogger<ReservationService> logger)
    {
        _db = db;
        _logger = logger;
    } 

    public async Task<IEnumerable<ReservationReadDto>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all reservations");
        var reservations = await _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .ToListAsync();

        return reservations.Select(r => r.ToReadDto());
    }

    public async Task<ReservationReadDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching reservation {ReservationId}", id);
        var reservation = await _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
            _logger.LogWarning("Reservation {ReservationId} not found", id);

        return reservation?.ToReadDto();
    }

    public async Task<ReservationReadDto?> CreateAsync(CreateReservationDto dto, Guid userId)
    {
        _logger.LogInformation("Creating reservation for user {UserId}, resource {ResourceId}, time {StartTime}-{EndTime}", userId, dto.ResourceId, dto.StartTime, dto.EndTime);

        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == dto.ResourceId);
        if (resource is null)
        {
            _logger.LogWarning("Resource {ResourceId} not found", dto.ResourceId);
            return null;
        }

        if (dto.StartTime >= dto.EndTime)
        {
            _logger.LogWarning("Invalid time range: start {Start} >= end {End}", dto.StartTime, dto.EndTime);
            return null;
        }

        var existingReservations = await _db.Reservations
            .Where(r => r.ResourceId == dto.ResourceId)
            .ToListAsync();

        var isOverlapping = existingReservations
            .Where(r => r.Status.DisplayName != "Cancelled")
            .Any(r => r.StartTime < dto.EndTime && r.EndTime > dto.StartTime);

        if (isOverlapping)
        {
            _logger.LogWarning("Overlapping reservation detected for resource {ResourceId}", dto.ResourceId);
            return null;
        }

        var reservation = dto.ToEntity(userId);
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reservation {ReservationId} created successfully", reservation.Id);

        _db.Entry(reservation).State = EntityState.Detached;
        return await GetByIdAsync(reservation.Id);
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        _logger.LogInformation("Cancelling reservation {ReservationId}", id);

        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == id);
        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found for cancellation", id);
            return false;
        }

        reservation.Status = new CancelledStatus();
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reservation {ReservationId} cancelled successfully", id);
        return true;
    }

    public async Task<IEnumerable<ReservationReadDto>> GetByUserIdAsync(Guid userId)
    {
        _logger.LogInformation("Fetching reservations for user {UserId}", userId);
        var reservations = await _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync();

        _logger.LogInformation("Found {Count} reservations for user {UserId}", reservations.Count(), userId);
        return reservations.Select(r => r.ToReadDto());
    }
}
