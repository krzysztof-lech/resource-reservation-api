using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Security;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<TokenDto?> AuthenticateAsync(LoginDto dto)
    {
        _logger.LogInformation("Authentication attempt for email {Email}", dto.Email);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed: user with email {Email} not found", dto.Email);
            return null;
        }

        if (!PasswordHasher.VerifyPassword(user.PasswordHash, dto.Password))
        {
            _logger.LogWarning("Login failed: invalid password for user {UserId} with email {Email}", user.Id, user.Email);
            return null;
        }
            

        var token = _tokenService.CreateToken(user);
        _logger.LogInformation("User {UserId} authenticated successfully, token issued", user.Id);

        return new TokenDto(token);
    }
}
