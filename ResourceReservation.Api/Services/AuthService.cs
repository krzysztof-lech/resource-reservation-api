using Microsoft.EntityFrameworkCore;
using ResourceReservation.Api.Data;
using ResourceReservation.Api.Dtos;
using ResourceReservation.Api.Security;
using ResourceReservation.Api.Services.Interfaces;

namespace ResourceReservation.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<TokenDto?> AuthenticateAsync(LoginDto dto)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null) return null;

        if (!PasswordHasher.VerifyPassword(user.PasswordHash, dto.Password))
            return null;

        var token = _tokenService.CreateToken(user);
        return new TokenDto(token);
    }
}
