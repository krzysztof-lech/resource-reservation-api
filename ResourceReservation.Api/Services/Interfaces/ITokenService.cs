using ResourceReservation.Api.Models;

namespace ResourceReservation.Api.Services.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}
