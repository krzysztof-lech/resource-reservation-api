using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;
namespace ResourceReservation.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService (AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<UserReadDto>> GetAllAsync()
    {
        var users = await _db.Users.AsNoTracking().ToListAsync();
        return users.Select(u => u.ToReadDto());
    }

    public async Task<UserReadDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user?.ToReadDto();
    }

    public async Task<UserReadDto?> CreateAsync(UserCreateDto dto)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists) return null;

        var user = dto.ToEntity();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user.ToReadDto();
    }

    public async Task<bool> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var existing = await _db.Users.FindAsync(id);
        if (existing is null) return false;
        existing.ApplyUpdates(dto);
        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _db.Users.FindAsync(id);
        if (existing is null) return false;
        _db.Users.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
