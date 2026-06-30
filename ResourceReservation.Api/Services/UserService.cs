using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Services.Interfaces;
namespace ResourceReservation.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService (AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<UserReadDto>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all users");
        var users = await _db.Users.AsNoTracking().ToListAsync();
        _logger.LogInformation("Found {Count} users", users.Count);
        return users.Select(u => u.ToReadDto());
    }

    public async Task<UserReadDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching user {UserId}", id);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            _logger.LogWarning("User {UserId} not found", id);

        return user?.ToReadDto();
    }

    public async Task<UserReadDto?> CreateAsync(UserCreateDto dto)
    {
        _logger.LogInformation("Creating user with email {Email}", dto.Email);

        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
        if (exists)
        {
            _logger.LogWarning("User with email {Email} already exists", dto.Email);
            return null;
        }

        var user = dto.ToEntity();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created successfully with email {Email}", user.Id, user.Email);
        return user.ToReadDto();
    }

    public async Task<bool> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        _logger.LogInformation("Updating user {UserId}", id);
        var existing = await _db.Users.FindAsync(id);

        if (existing is null)
        {
            _logger.LogWarning("User {UserId} not found for update", id);
            return false;
        }
        existing.ApplyUpdates(dto);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated successfully", id);
        return true;
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting user {UserId}", id);
        var existing = await _db.Users.FindAsync(id);

        if (existing is null)
        {
            _logger.LogWarning("User {UserId} not found for deletion", id);
            return false;
        }
        _db.Users.Remove(existing);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted successfully", id);
        return true;
    }
}
