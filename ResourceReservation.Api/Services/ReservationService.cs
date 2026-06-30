using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _db;

    public ReservationService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ReservationReadDto>> GetAllAsync()
    {
        var reservations = await _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .ToListAsync();

        return reservations.Select(r => r.ToReadDto());
    }

    public async Task<ReservationReadDto?> GetByIdAsync(Guid id)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return reservation?.ToReadDto();
    }

    public async Task<ReservationReadDto?> CreateAsync(CreateReservationDto dto, Guid userId)
    {
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == dto.ResourceId);
        if (resource is null) return null;

        if (dto.StartTime >= dto.EndTime) return null;

        var existingReservations = await _db.Reservations
            .Where(r => r.ResourceId == dto.ResourceId)
            .ToListAsync();

        var isOverlapping = existingReservations
            .Where(r => r.Status.DisplayName != "Cancelled")
            .Any(r => r.StartTime < dto.EndTime && r.EndTime > dto.StartTime);

        if (isOverlapping) return null;

        var reservation = dto.ToEntity(userId);
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        _db.Entry(reservation).State = EntityState.Detached;
        return await GetByIdAsync(reservation.Id);
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == id);
        if (reservation is null) return false;

        reservation.Status = new CancelledStatus();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ReservationReadDto>> GetByUserIdAsync(Guid userId)
    {
        var reservations = await _db.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync();

        return reservations.Select(r => r.ToReadDto());
    }
}
