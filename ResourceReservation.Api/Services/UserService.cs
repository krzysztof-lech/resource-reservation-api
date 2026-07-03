using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Models;
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

    public async Task<IEnumerable<UserReadDto>> SearchAsync(string? q = null, string? role = null, DateTime? createdAfter = null, DateTime? createdBefore = null, int? page = null, int? pageSize = null)
    {
        _logger.LogInformation("Searching users q='{Q}', role={Role}, createdAfter={CreatedAfter}, createdBefore={CreatedBefore}, page={Page}, pageSize={PageSize}", q, role, createdAfter, createdBefore, page, pageSize);

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(u =>
                u.FirstName.Contains(normalized) ||
                u.LastName.Contains(normalized) ||
                u.Email.Contains(normalized)
            );
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
            {
                query = query.Where(u => u.Role == parsedRole);
            }
            else
            {
                return Enumerable.Empty<UserReadDto>();
            }
        }

        if (createdAfter.HasValue)
            query = query.Where(u => u.CreatedAt >= createdAfter.Value);

        if (createdBefore.HasValue)
            query = query.Where(u => u.CreatedAt <= createdBefore.Value);

        if (pageSize.HasValue && pageSize > 0)
        {
            query = query.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id);

            var p = page.GetValueOrDefault(1);
            query = query.Skip((p - 1) * pageSize.Value).Take(pageSize.Value);
        }
        else
        {
            query = query.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id);
        }

        var users = await query.ToListAsync();
        _logger.LogInformation("Search returned {Count} users", users.Count);
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
