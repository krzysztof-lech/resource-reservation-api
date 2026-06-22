using ResourceReservation.Api.Dtos;

namespace ResourceReservation.Api.Services.Interfaces;

public interface IAuthService
{
    Task<TokenDto?> AuthenticateAsync(LoginDto dto);
}
