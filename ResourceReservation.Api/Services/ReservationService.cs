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

    public async Task<IEnumerable<IReservationReadDto>> SearchAsync(bool isAdmin, Guid? userId = null, string? status = null, bool? isPast = null)
    {
        _logger.LogInformation("Searching reservations: userId={UserId}, status={Status}, isPast={IsPast}", userId, status, isPast);

        var query = _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(r => r.Status.DisplayName.Contains(normalizedStatus));
        }

        if (isPast.HasValue)
        {
            var now = DateTime.UtcNow;
            if (isPast.Value)
            {
                query = query.Where(r => r.EndTime < now);
            }
            else
            {
                query = query.Where(r => r.EndTime >= now);
            }
        }

        query = query.OrderByDescending(r => r.StartTime);

        var results = await query.ToListAsync();

        _logger.LogInformation("Search returned {Count} reservations", results.Count);

        return isAdmin
            ? results.Select(r => r.ToReadDto())
            : results.Select(r => r.ToPublicReadDto());
    }

    public async Task<IReservationReadDto?> GetByIdAsync(Guid id, bool isAdmin)
    {
        _logger.LogInformation("Fetching reservation {ReservationId}", id);
        var reservation = await _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", id);
            return null;
        }

        return isAdmin
            ? reservation.ToReadDto()
            : reservation.ToPublicReadDto();
    }

    public async Task<IReservationReadDto?> CreateAsync(CreateReservationDto dto, Guid userId)
    {
        _logger.LogInformation("Creating reservation for user {UserId}, resource {ResourceId}, time {StartTime}-{EndTime}", userId, dto.ResourceId, dto.StartTime, dto.EndTime);

        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == dto.ResourceId);
        if (resource is null)
        {
            _logger.LogWarning("Resource {ResourceId} not found", dto.ResourceId);
            return null;
        }

        if (!resource.IsAvailable)
        {
            _logger.LogWarning("Resource {ResourceId} is currently deactivated/unavailable", dto.ResourceId);
            return null;
        }


        var reservationDay = dto.StartTime.DayOfWeek;
        if (!resource.AllowedDays.Contains(reservationDay))
        {
            _logger.LogWarning("Resource {ResourceId} is not available on {DayOfWeek}", dto.ResourceId, reservationDay);
            return null;
        }

        var rStart = TimeOnly.FromDateTime(dto.StartTime);
        var rEnd = TimeOnly.FromDateTime(dto.EndTime);
        if (rStart < resource.AvailableFrom || rEnd > resource.AvailableTo)
        {
            _logger.LogWarning("Reservation time {Start}-{End} is outside of resource operating hours {AvailableFrom}-{AvailableTo}", rStart, rEnd, resource.AvailableFrom, resource.AvailableTo);
            return null;
        }

        var futureReservations = await _db.Reservations
            .Where(r => r.ResourceId == dto.ResourceId && r.EndTime >= DateTime.UtcNow)
            .Select(r => new { r.StartTime, r.EndTime, StatusName = r.Status.DisplayName })
            .ToListAsync();

        var isOverlapping = futureReservations
            .Where(r => r.StatusName != "Cancelled")
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
        return await GetByIdAsync(reservation.Id, false);
    }

    public async Task<CancelReservationResult> CancelAsync(Guid id, Guid requesterId, bool isAdmin)
    {
        _logger.LogInformation("Cancel requested for reservation {ReservationId} by {RequesterId} (isAdmin={IsAdmin})", id, requesterId, isAdmin);

        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found for cancellation", id);
            return CancelReservationResult.NotFound;
        }

        if (!isAdmin && reservation.UserId != requesterId)
        {
            _logger.LogWarning("Requester {RequesterId} attempted to cancel reservation {ReservationId} owned by {OwnerId}", requesterId, id, reservation.UserId);
            return CancelReservationResult.Forbidden;
        }

        var targetStatus = new CancelledStatus();
        if (!reservation.Status.CanTransitionTo(targetStatus))
        {
            _logger.LogWarning("Reservation {ReservationId} cannot transition from {CurrentStatus} to Cancelled", id, reservation.Status.DisplayName);
            return CancelReservationResult.CannotCancel;
        }

        reservation.Status = targetStatus;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reservation {ReservationId} cancelled successfully", id);
        return CancelReservationResult.Success;
    }

    public async Task<ConfirmReservationResult> ConfirmAsync(Guid id)
    {
        _logger.LogInformation("Confirm requested for reservation {ReservationId}", id);

        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found for confirmation", id);
            return ConfirmReservationResult.NotFound;
        }

        var targetStatus = new ConfirmedStatus();
        if (!reservation.Status.CanTransitionTo(targetStatus))
        {
            _logger.LogWarning("Reservation {ReservationId} cannot transition from {CurrentStatus} to Confirmed", id, reservation.Status.DisplayName);
            return ConfirmReservationResult.CannotConfirm;
        }

        reservation.Status = targetStatus;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reservation {ReservationId} confirmed successfully", id);
        return ConfirmReservationResult.Success;
    }
}
